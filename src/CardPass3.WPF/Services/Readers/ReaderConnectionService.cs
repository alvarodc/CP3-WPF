using CardPass3.WPF.Data.Models;
using CardPass3.WPF.Data.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace CardPass3.WPF.Services.Readers
{

// ─── Connection state ────────────────────────────────────────────────────────

public enum ReaderConnectionState
{
    Idle,
    Connecting,
    Connected,
    Failed,
    Disconnected
}

public class ReaderConnectionInfo
{
    public Reader Reader { get; init; } = null!;
    public ReaderConnectionState State { get; set; } = ReaderConnectionState.Idle;
    public string? ErrorMessage { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>Convenience: controls that depend on this reader should be enabled only when Connected.</summary>
    public bool IsOperational => State == ReaderConnectionState.Connected;
}

// ─── Service interface ───────────────────────────────────────────────────────

public interface IReaderConnectionService
{
    /// <summary>Observable collection updated on the UI thread as connections resolve.</summary>
    ReadOnlyObservableCollection<ReaderConnectionInfo> Readers { get; }

    /// <summary>True while initial connection sweep is still in progress.</summary>
    bool IsConnecting { get; }

    /// <summary>
    /// Starts connecting all enabled readers in parallel (fire-and-forget from startup).
    /// Progress is reflected via the Readers collection.
    /// </summary>
    Task StartAsync(CancellationToken ct = default);

    Task ConnectReaderAsync(int readerId, CancellationToken ct = default);
    Task DisconnectReaderAsync(int readerId, CancellationToken ct = default);
    Task DisconnectAllAsync(CancellationToken ct = default);

    ReaderConnectionInfo? GetInfo(int readerId);
}

// ─── Service implementation ──────────────────────────────────────────────────

public class ReaderConnectionService : IReaderConnectionService
{
    private readonly IReaderRepository _readerRepo;
    private readonly IReaderDriverFactory _driverFactory;
    private readonly ILogger<ReaderConnectionService> _logger;

    // Concurrent dictionary: readerId → connection info
    private readonly ConcurrentDictionary<int, ReaderConnectionInfo> _infoMap = new();

    // Backing collection (updated on UI thread via dispatcher)
    private readonly ObservableCollection<ReaderConnectionInfo> _readers = new();
    public ReadOnlyObservableCollection<ReaderConnectionInfo> Readers { get; }

    public bool IsConnecting { get; private set; }

    // Max simultaneous TCP connection attempts (prevent SYN storm with 100+ readers)
    private const int MaxParallelConnections = 10;

    public ReaderConnectionService(
        IReaderRepository readerRepo,
        IReaderDriverFactory driverFactory,
        ILogger<ReaderConnectionService> logger)
    {
        _readerRepo = readerRepo;
        _driverFactory = driverFactory;
        _logger = logger;
        Readers = new ReadOnlyObservableCollection<ReaderConnectionInfo>(_readers);
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        IsConnecting = true;

        try
        {
            var enabledReaders = (await _readerRepo.GetAllEnabledAsync(ct)).ToList();
            _logger.LogInformation("Starting connection to {Count} enabled readers.", enabledReaders.Count);

            // Pre-populate the collection in Idle state so UI can show all readers immediately
            foreach (var reader in enabledReaders)
            {
                var info = new ReaderConnectionInfo { Reader = reader, State = ReaderConnectionState.Idle };
                _infoMap[reader.IdReader] = info;
                App.Current.Dispatcher.Invoke(() => _readers.Add(info));
            }

            // Connect in parallel, respecting the semaphore limit
            using var semaphore = new SemaphoreSlim(MaxParallelConnections, MaxParallelConnections);

            var tasks = enabledReaders.Select(reader => Task.Run(async () =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    await ConnectReaderCoreAsync(_infoMap[reader.IdReader], ct);
                }
                finally
                {
                    semaphore.Release();
                }
            }, ct));

            await Task.WhenAll(tasks);

            _logger.LogInformation(
                "Reader connection sweep complete. Connected: {C}, Failed: {F}",
                _infoMap.Values.Count(i => i.State == ReaderConnectionState.Connected),
                _infoMap.Values.Count(i => i.State == ReaderConnectionState.Failed));
        }
        finally
        {
            IsConnecting = false;
        }
    }

    public async Task ConnectReaderAsync(int readerId, CancellationToken ct = default)
    {
        if (!_infoMap.TryGetValue(readerId, out var info))
        {
            var reader = await _readerRepo.GetByIdAsync(readerId, ct)
                ?? throw new InvalidOperationException($"Reader {readerId} not found.");
            info = new ReaderConnectionInfo { Reader = reader };
            _infoMap[readerId] = info;
            App.Current.Dispatcher.Invoke(() => _readers.Add(info));
        }

        await ConnectReaderCoreAsync(info, ct);
    }

    public async Task DisconnectReaderAsync(int readerId, CancellationToken ct = default)
    {
        if (!_infoMap.TryGetValue(readerId, out var info)) return;

        try
        {
            var driver = _driverFactory.GetDriver(info.Reader);
            await driver.DisconnectAsync(ct);
            UpdateState(info, ReaderConnectionState.Disconnected);
            _logger.LogInformation("Reader {Id} ({Desc}) disconnected.", readerId, info.Reader.ReaderDescription);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disconnecting reader {Id}.", readerId);
        }
    }

    public async Task DisconnectAllAsync(CancellationToken ct = default)
    {
        var tasks = _infoMap.Keys.Select(id => DisconnectReaderAsync(id, ct));
        await Task.WhenAll(tasks);
    }

    public ReaderConnectionInfo? GetInfo(int readerId)
        => _infoMap.TryGetValue(readerId, out var info) ? info : null;

    // ─── Private ─────────────────────────────────────────────────────────────

    private async Task ConnectReaderCoreAsync(ReaderConnectionInfo info, CancellationToken ct)
    {
        UpdateState(info, ReaderConnectionState.Connecting);
        info.LastAttemptAt = DateTime.UtcNow;

        try
        {
            var driver = _driverFactory.GetDriver(info.Reader);
            await driver.ConnectAsync(ct);

            info.ConnectedAt = DateTime.UtcNow;
            info.ErrorMessage = null;
            UpdateState(info, ReaderConnectionState.Connected);

            _logger.LogInformation(
                "Reader {Id} ({Desc}) connected at {Ip}:{Port}.",
                info.Reader.IdReader,
                info.Reader.ReaderDescription,
                info.Reader.EffectiveIp,
                info.Reader.Port);
        }
        catch (OperationCanceledException)
        {
            UpdateState(info, ReaderConnectionState.Failed);
            info.ErrorMessage = "Connection cancelled.";
        }
        catch (Exception ex)
        {
            UpdateState(info, ReaderConnectionState.Failed);
            info.ErrorMessage = ex.Message;

            _logger.LogWarning(
                "Reader {Id} ({Ip}:{Port}) connection failed: {Error}",
                info.Reader.IdReader,
                info.Reader.EffectiveIp,
                info.Reader.Port,
                ex.Message);
        }
    }

    /// <summary>Updates state on the UI thread so bound UI refreshes automatically.</summary>
    private static void UpdateState(ReaderConnectionInfo info, ReaderConnectionState state)
    {
        App.Current.Dispatcher.Invoke(() => info.State = state);
    }
}
}
