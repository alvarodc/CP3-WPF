using CardPass3.WPF.Data.Models;
using CardPass3.WPF.Data.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace CardPass3.WPF.Services.Readers;

/// <summary>
/// Detecta cambios en la tabla readers realizados desde otros puestos de la instalación
/// y aplica el diff al ReaderConnectionService local.
///
/// Estrategia: polling periódico comparando el snapshot actual en memoria contra BD.
/// Simple, sin dependencias externas, suficiente para el volumen de cambios esperado
/// (altas/bajas/modificaciones de lectores son operaciones ocasionales).
///
/// Intervalo por defecto: 5 segundos.
/// </summary>
public sealed class ReaderSyncService : IAsyncDisposable
{
    private readonly ReaderConnectionService    _connectionService;
    private readonly IReaderRepository          _readerRepo;
    private readonly ILogger<ReaderSyncService> _logger;

    private readonly TimeSpan          _pollInterval;
    private CancellationTokenSource?   _cts;
    private Task?                      _loopTask;

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
        _logger.LogInformation("ReaderSyncService started (interval: {Interval}s)", _pollInterval.TotalSeconds);
    }

    public async Task StopAsync()
    {
        if (_cts is null) return;
        _cts.Cancel();
        if (_loopTask is not null)
            await _loopTask.ConfigureAwait(false);
        _cts.Dispose();
    }

    private async Task RunAsync(CancellationToken ct)
    {
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
                _logger.LogWarning(ex, "ReaderSyncService error during poll");
                // No re-throw — el loop continúa en el siguiente tick
            }
        }
    }

    private async Task CheckAndApplyDiffAsync(CancellationToken ct)
    {
        // No comparar mientras el servicio aún está cargando la lista inicial,
        // para evitar duplicados y ArgumentException en el ToDictionary.
        if (_connectionService.IsStarting) return;

        // Estado actual en memoria — usamos GroupBy para ser robustos ante
        // cualquier duplicado residual que pudiera existir en la colección.
        var current = _connectionService.Readers
            .GroupBy(r => r.Reader.IdReader)
            .ToDictionary(g => g.Key, g => g.First().Reader);

        // Estado en BD (incluyendo soft-deleted para detectar bajas)
        var dbReaders = (await _readerRepo.GetAllAsync(ct)).ToList();
        var dbMap     = dbReaders
            .Where(r => !r.Deleted)
            .GroupBy(r => r.IdReader)
            .ToDictionary(g => g.Key, g => g.First());

        var diff = new ReaderSyncDiff();

        // Detectar altas y modificaciones
        foreach (var (id, dbReader) in dbMap)
        {
            if (!current.TryGetValue(id, out var local))
            {
                diff.Added.Add(dbReader);
            }
            else if (HasChanged(local, dbReader))
            {
                diff.Updated.Add(dbReader);
            }
        }

        // Detectar bajas (en memoria pero no en BD, o marcados como deleted)
        foreach (var id in current.Keys)
        {
            if (!dbMap.ContainsKey(id))
                diff.RemovedIds.Add(id);
        }

        if (diff.HasChanges)
        {
            _logger.LogInformation(
                "[Sync] Changes detected — added: {A}, updated: {U}, removed: {R}",
                diff.Added.Count, diff.Updated.Count, diff.RemovedIds.Count);

            await _connectionService.ApplySyncDiffAsync(diff, ct);
        }
    }

    /// <summary>
    /// Compara los campos relevantes para determinar si hay que reconectar.
    /// Cambios en descripción o área no requieren reconexión.
    /// </summary>
    private static bool HasChanged(Reader local, Reader db)
        => local.IpAddress          != db.IpAddress
        || local.IpAddressEffective != db.IpAddressEffective
        || local.Port               != db.Port
        || local.UniqueName         != db.UniqueName
        || local.Enabled            != db.Enabled
        || local.Driver             != db.Driver;

    public async ValueTask DisposeAsync() => await StopAsync();
}
