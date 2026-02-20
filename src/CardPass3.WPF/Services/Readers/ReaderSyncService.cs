using CardPass3.WPF.Data.Models;
using CardPass3.WPF.Data.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace CardPass3.WPF.Services.Readers;

/// <summary>
/// Detecta cambios en la tabla readers realizados desde otros puestos
/// y aplica el diff al ReaderConnectionService local.
///
/// Nota: el sync no arranca hasta que ReaderConnectionService.StartAsync()
/// haya terminado, evitando race conditions con la carga inicial.
/// </summary>
public sealed class ReaderSyncService : IAsyncDisposable
{
    private readonly ReaderConnectionService    _connectionService;
    private readonly IReaderRepository          _readerRepo;
    private readonly ILogger<ReaderSyncService> _logger;

    private readonly TimeSpan        _pollInterval;
    private CancellationTokenSource? _cts;
    private Task?                    _loopTask;

    public ReaderSyncService(
        ReaderConnectionService    connectionService,
        IReaderRepository          readerRepo,
        ILogger<ReaderSyncService> logger,
        TimeSpan?                  pollInterval = null)
    {
        _connectionService = connectionService;
        _readerRepo        = readerRepo;
        _logger            = logger;
        _pollInterval      = pollInterval ?? TimeSpan.FromSeconds(5);
    }

    public void Start()
    {
        _cts      = new CancellationTokenSource();
        _loopTask = RunAsync(_cts.Token);
        _logger.LogInformation("ReaderSyncService started (interval: {Interval}s)",
            _pollInterval.TotalSeconds);
    }

    public async Task StopAsync()
    {
        if (_cts is null) return;
        _cts.Cancel();
        if (_loopTask is not null)
            await _loopTask.ConfigureAwait(false);
        _cts.Dispose();
        _cts = null;
    }

    private async Task RunAsync(CancellationToken ct)
    {
        // Esperar a que la carga inicial de lectores termine antes de empezar a hacer diff.
        // Sin esto, el sync ve 0 lectores en memoria y los añade todos como "Added",
        // causando duplicados y el ArgumentException en ToDictionary.
        while (_connectionService.IsStarting && !ct.IsCancellationRequested)
            await Task.Delay(200, ct).ConfigureAwait(false);

        if (ct.IsCancellationRequested) return;

        _logger.LogDebug("ReaderSyncService: initial load complete, starting poll loop");

        using var timer = new PeriodicTimer(_pollInterval);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(ct);
                await CheckAndApplyDiffAsync(ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ReaderSyncService: error during poll, continuing");
            }
        }
    }

    private async Task CheckAndApplyDiffAsync(CancellationToken ct)
    {
        // Snapshot en memoria — GroupBy + First por si hubiera duplicados residuales
        var current = _connectionService.Readers
            .GroupBy(r => r.Reader.IdReader)
            .ToDictionary(g => g.Key, g => g.First().Reader);

        // Estado en BD (todos, para detectar soft-deletes)
        var dbReaders = (await _readerRepo.GetAllAsync(ct)).ToList();
        var dbMap = dbReaders
            .Where(r => !r.Deleted)
            .GroupBy(r => r.IdReader)
            .ToDictionary(g => g.Key, g => g.First());

        var diff = new ReaderSyncDiff();

        foreach (var (id, dbReader) in dbMap)
        {
            if (!current.TryGetValue(id, out var local))
                diff.Added.Add(dbReader);
            else if (HasChanged(local, dbReader))
                diff.Updated.Add(dbReader);
        }

        foreach (var id in current.Keys)
        {
            if (!dbMap.ContainsKey(id))
                diff.RemovedIds.Add(id);
        }

        if (diff.HasChanges)
        {
            _logger.LogInformation(
                "[Sync] Changes from another instance — added:{A} updated:{U} removed:{R}",
                diff.Added.Count, diff.Updated.Count, diff.RemovedIds.Count);

            await _connectionService.ApplySyncDiffAsync(diff, ct);
        }
    }

    private static bool HasChanged(Reader local, Reader db)
        => local.IpAddress          != db.IpAddress
        || local.IpAddressEffective != db.IpAddressEffective
        || local.Port               != db.Port
        || local.UniqueName         != db.UniqueName
        || local.Enabled            != db.Enabled
        || local.Driver             != db.Driver;

    public async ValueTask DisposeAsync() => await StopAsync();
}
