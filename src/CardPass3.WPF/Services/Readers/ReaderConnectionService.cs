using CardPass3.WPF.Data.Models;
using CardPass3.WPF.Data.Repositories.Interfaces;
using CardPass3.WPF.Services.Readers.Lmpi;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Windows;

namespace CardPass3.WPF.Services.Readers;

/// <summary>
/// Orquestador central de conexiones a lectores.
/// La lista en memoria refleja TODOS los lectores no borrados (enabled o no),
/// para que el sync multi-instancia pueda comparar correctamente sin duplicados.
/// </summary>
public sealed class ReaderConnectionService : IReaderConnectionService, IAsyncDisposable
{
    private readonly IReaderRepository                _readerRepo;
    private readonly IConfigurationRepository         _configRepo;
    private readonly ILogger<ReaderConnectionService> _logger;
    private readonly ILoggerFactory                   _loggerFactory;

    private readonly ConcurrentDictionary<int, LmpiDriver> _drivers = new();
    private readonly ObservableCollection<ReaderConnectionInfo> _readers = new();

    public ObservableCollection<ReaderConnectionInfo> Readers => _readers;
    public bool IsStarting { get; private set; }

    private int _startCalled = 0; // Guard para evitar doble arranque

    private const int MaxParallelAtStartup = 10;

    public event Action<ReaderConnectionInfo, LmpiEvent> EventReceived          = delegate { };
    public event Action<ReaderConnectionInfo>             ConnectionStateChanged = delegate { };

    public ReaderConnectionService(
        IReaderRepository        readerRepo,
        IConfigurationRepository configRepo,
        ILoggerFactory           loggerFactory)
    {
        _readerRepo    = readerRepo;
        _configRepo    = configRepo;
        _loggerFactory = loggerFactory;
        _logger        = loggerFactory.CreateLogger<ReaderConnectionService>();
    }

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    public async Task StartAsync(CancellationToken ct = default)
    {
        // Evitar que StartAsync se ejecute más de una vez (p.ej. por llamadas concurrentes en el arranque)
        if (Interlocked.Exchange(ref _startCalled, 1) != 0)
        {
            _logger.LogWarning("StartAsync called more than once — ignoring duplicate call.");
            return;
        }

        IsStarting = true;
        try
        {
            var allReaders = (await _readerRepo.GetAllAsync(ct)).ToList();
            _logger.LogInformation(
                "Loading {Total} readers ({Enabled} enabled)",
                allReaders.Count,
                allReaders.Count(r => r.Enabled));

            foreach (var reader in allReaders)
            {
                // Doble check: no añadir si ya existe (defensivo ante cualquier race residual)
                if (_readers.All(r => r.Reader.IdReader != reader.IdReader))
                    OnUiThread(() => _readers.Add(CreateInfo(reader)));
            }

            // Solo conectar los habilitados, en paralelo
            var toConnect = allReaders.Where(r => r.Enabled).ToList();
            using var semaphore = new SemaphoreSlim(MaxParallelAtStartup);

            var tasks = toConnect.Select(r => Task.Run(async () =>
            {
                await semaphore.WaitAsync(ct);
                try   { await StartDriverAsync(r, ct); }
                finally { semaphore.Release(); }
            }, ct));

            await Task.WhenAll(tasks);

            _logger.LogInformation("Startup complete — connected: {C}/{T}",
                _drivers.Values.Count(d => d.IsReaderConnected), _drivers.Count);
        }
        finally { IsStarting = false; }
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Stopping all reader connections");
        await Task.WhenAll(_drivers.Values.Select(d => d.DisposeAsync().AsTask()));
        _drivers.Clear();
        OnUiThread(_readers.Clear);
    }

    // ── Operaciones individuales ──────────────────────────────────────────────

    public async Task ConnectAsync(int readerId, CancellationToken ct = default)
    {
        if (_drivers.TryRemove(readerId, out var old)) await old.DisposeAsync();

        var reader = await _readerRepo.GetByIdAsync(readerId, ct)
            ?? throw new InvalidOperationException($"Reader {readerId} not found.");

        if (!_readers.Any(r => r.Reader.IdReader == readerId))
            OnUiThread(() => _readers.Add(CreateInfo(reader)));

        await StartDriverAsync(reader, ct);
    }

    public async Task DisconnectAsync(int readerId, CancellationToken ct = default)
    {
        if (!_drivers.TryRemove(readerId, out var driver)) return;
        await driver.DisposeAsync();
        UpdateInfo(readerId, info =>
        {
            info.State        = ReaderConnectionState.Disconnected;
            info.ErrorMessage = null;
        });
    }

    // ── Comandos al lector ────────────────────────────────────────────────────

    public void OpenRelay(int readerId)    { if (_drivers.TryGetValue(readerId, out var d)) d.OpenOnce(); }
    public void Restart(int readerId)      { if (_drivers.TryGetValue(readerId, out var d)) d.Restart(); }
    public void EmergencyOpen()            { foreach (var d in _drivers.Values) d.Emergency(); }
    public void EmergencyEnd()             { foreach (var d in _drivers.Values) d.EmergencyEnd(); }

    // ── CRUD ──────────────────────────────────────────────────────────────────

    public async Task<ReaderConnectionInfo> AddReaderAsync(Reader reader, CancellationToken ct = default)
    {
        await _readerRepo.InsertAsync(reader, ct);
        // Releer de BD para obtener el IdReader generado
        var saved = (await _readerRepo.GetAllAsync(ct))
            .OrderByDescending(r => r.IdReader)
            .First(r => r.UniqueName == reader.UniqueName);

        var info = CreateInfo(saved);
        OnUiThread(() => _readers.Add(info));
        if (saved.Enabled) await StartDriverAsync(saved, ct);
        return info;
    }

    public async Task UpdateReaderAsync(Reader reader, CancellationToken ct = default)
    {
        await _readerRepo.UpdateAsync(reader, ct);

        // Desconectar driver actual (IP/puerto puede haber cambiado)
        if (_drivers.TryRemove(reader.IdReader, out var old)) await old.DisposeAsync();

        UpdateInfo(reader.IdReader, info =>
        {
            info.Reader = reader;
            info.State  = ReaderConnectionState.Idle;
        });

        // Reconectar solo si está habilitado
        if (reader.Enabled)
            await StartDriverAsync(reader, ct);
    }

    public async Task RemoveReaderAsync(int readerId, CancellationToken ct = default)
    {
        if (_drivers.TryRemove(readerId, out var driver)) await driver.DisposeAsync();
        await _readerRepo.SoftDeleteAsync(readerId, ct);
        OnUiThread(() =>
        {
            var item = _readers.FirstOrDefault(r => r.Reader.IdReader == readerId);
            if (item is not null) _readers.Remove(item);
        });
    }

    public async Task SetEnabledAsync(int readerId, bool enabled, CancellationToken ct = default)
    {
        var reader = await _readerRepo.GetByIdAsync(readerId, ct);
        if (reader is null) return;

        reader.Enabled = enabled;
        await _readerRepo.UpdateAsync(reader, ct);

        if (!enabled)
        {
            if (_drivers.TryRemove(readerId, out var driver)) await driver.DisposeAsync();
            UpdateInfo(readerId, info =>
            {
                info.Reader  = reader;
                info.State   = ReaderConnectionState.Disconnected;
            });
        }
        else
        {
            UpdateInfo(readerId, info => info.Reader = reader);
            await StartDriverAsync(reader, ct);
        }
    }

    // ── Sincronización multi-instancia ────────────────────────────────────────

    internal async Task ApplySyncDiffAsync(ReaderSyncDiff diff, CancellationToken ct)
    {
        foreach (var reader in diff.Added)
        {
            _logger.LogInformation("[Sync] Reader {Id} added from another instance", reader.IdReader);
            var info = CreateInfo(reader);
            OnUiThread(() => _readers.Add(info));
            if (reader.Enabled) await StartDriverAsync(reader, ct);
        }

        foreach (var reader in diff.Updated)
        {
            _logger.LogInformation("[Sync] Reader {Id} updated from another instance", reader.IdReader);
            if (_drivers.TryRemove(reader.IdReader, out var old)) await old.DisposeAsync();
            UpdateInfo(reader.IdReader, info => { info.Reader = reader; info.State = ReaderConnectionState.Idle; });
            if (reader.Enabled) await StartDriverAsync(reader, ct);
        }

        foreach (var id in diff.RemovedIds)
        {
            _logger.LogInformation("[Sync] Reader {Id} removed from another instance", id);
            if (_drivers.TryRemove(id, out var driver)) await driver.DisposeAsync();
            OnUiThread(() =>
            {
                var item = _readers.FirstOrDefault(r => r.Reader.IdReader == id);
                if (item is not null) _readers.Remove(item);
            });
        }
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private async Task StartDriverAsync(Reader reader, CancellationToken ct)
    {
        var retryInterval = await GetRetryIntervalAsync(ct);
        var ip            = await GetEffectiveIpAsync(reader, ct);

        var driver = new LmpiDriver(ip, reader.Port, reader.UniqueName ?? reader.ReaderDescription,
            retryInterval, _loggerFactory.CreateLogger<LmpiDriver>());

        driver.ConnectionStateChanged += (_, state) => OnDriverStateChanged(reader.IdReader, state);
        driver.EventReceived          += (_, ev)    => OnDriverEvent(reader.IdReader, ev);
        driver.AppStateChanged        += (_, state) => OnDriverAppState(reader.IdReader, state);
        driver.ReaderStateChanged     += (_, state) => OnDriverReaderState(reader.IdReader, state);

        _drivers[reader.IdReader] = driver;

        UpdateInfo(reader.IdReader, info =>
        {
            info.State         = ReaderConnectionState.Connecting;
            info.LastAttemptAt = DateTime.UtcNow;
            info.ErrorMessage  = null;
        });

        driver.StartConnect();
    }

    private void OnDriverStateChanged(int readerId, TcpState tcpState)
    {
        var mapped = tcpState switch
        {
            TcpState.Disconnected    => ReaderConnectionState.Disconnected,
            TcpState.Connecting      => ReaderConnectionState.Connecting,
            TcpState.TcpConnected    => ReaderConnectionState.TcpConnected,
            TcpState.ReaderConnected => ReaderConnectionState.ReaderConnected,
            TcpState.Disconnecting   => ReaderConnectionState.Disconnected,
            _                        => ReaderConnectionState.Failed
        };

        UpdateInfo(readerId, info =>
        {
            info.State = mapped;
            if (mapped == ReaderConnectionState.ReaderConnected)
            { info.ConnectedAt = DateTime.UtcNow; info.ErrorMessage = null; }
        });

        var info = _readers.FirstOrDefault(r => r.Reader.IdReader == readerId);
        if (info is not null) SafeRaise(() => ConnectionStateChanged(info));
    }

    private void OnDriverEvent(int readerId, LmpiEvent ev)
    {
        var info = _readers.FirstOrDefault(r => r.Reader.IdReader == readerId);
        if (info is not null) SafeRaise(() => EventReceived(info, ev));
    }

    private void OnDriverAppState(int readerId, AppState state)
    {
        UpdateInfo(readerId, info => info.AppState = state);
        var info = _readers.FirstOrDefault(r => r.Reader.IdReader == readerId);
        if (info is not null) SafeRaise(() => ConnectionStateChanged(info));
    }

    private void OnDriverReaderState(int readerId, ReaderState state)
    {
        UpdateInfo(readerId, info => info.ReaderState = state);
        var info = _readers.FirstOrDefault(r => r.Reader.IdReader == readerId);
        if (info is not null) SafeRaise(() => ConnectionStateChanged(info));
    }

    private async Task<TimeSpan> GetRetryIntervalAsync(CancellationToken ct)
    {
        var raw = await _configRepo.GetValueAsync("connectionRetriesIntervalSeconds", ct);
        return int.TryParse(raw, out var s) && s > 0 ? TimeSpan.FromSeconds(s) : TimeSpan.FromSeconds(30);
    }

    private async Task<string> GetEffectiveIpAsync(Reader reader, CancellationToken ct)
    {
        var flag = await _configRepo.GetValueAsync("use_ip_address_effective", ct);
        return flag == "1" && !string.IsNullOrWhiteSpace(reader.IpAddressEffective)
            ? reader.IpAddressEffective : reader.IpAddress;
    }

    private static ReaderConnectionInfo CreateInfo(Reader reader) =>
        new() { Reader = reader, State = ReaderConnectionState.Idle };

    private void UpdateInfo(int readerId, Action<ReaderConnectionInfo> update)
        => OnUiThread(() =>
        {
            var info = _readers.FirstOrDefault(r => r.Reader.IdReader == readerId);
            if (info is not null) update(info);
        });

    private static void OnUiThread(Action action)
    {
        if (Application.Current?.Dispatcher is { } d) d.Invoke(action);
        else action();
    }

    private void SafeRaise(Action action)
    {
        try { action(); }
        catch (Exception ex) { _logger.LogError(ex, "Error in reader event handler"); }
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}

internal sealed class ReaderSyncDiff
{
    public List<Reader> Added      { get; init; } = [];
    public List<Reader> Updated    { get; init; } = [];
    public List<int>    RemovedIds { get; init; } = [];
    public bool HasChanges => Added.Count > 0 || Updated.Count > 0 || RemovedIds.Count > 0;
}
