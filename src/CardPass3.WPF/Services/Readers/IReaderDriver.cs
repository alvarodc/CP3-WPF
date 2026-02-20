using CardPass3.WPF.Data.Models;
using CardPass3.WPF.Services.Readers.Lmpi;
using System.Collections.ObjectModel;

namespace CardPass3.WPF.Services.Readers;

// ─── Connection state (UI-facing, maps from TcpState) ────────────────────────

public enum ReaderConnectionState
{
    Idle,
    Connecting,
    TcpConnected,      // TCP OK pero lector aún no confirmado
    ReaderConnected,   // Operativo al 100%
    Failed,
    Disconnected
}

// ─── Runtime info per reader (bound to UI grid) ──────────────────────────────

public sealed class ReaderConnectionInfo
{
    public required Reader Reader         { get; set; }
    public ReaderConnectionState State    { get; set; } = ReaderConnectionState.Idle;
    public AppState    AppState           { get; set; } = AppState.Control;
    public ReaderState ReaderState        { get; set; } = ReaderState.Control;
    public string?     ErrorMessage       { get; set; }
    public DateTime?   ConnectedAt        { get; set; }
    public DateTime?   LastAttemptAt      { get; set; }

    public bool IsOperational => State == ReaderConnectionState.ReaderConnected;
    public bool IsEmergency   => AppState  == AppState.Emergency
                              || ReaderState == ReaderState.Emergency;
}

// ─── Service interface ────────────────────────────────────────────────────────

public interface IReaderConnectionService
{
    /// <summary>Lista observable actualizada en el hilo de UI.</summary>
    ObservableCollection<ReaderConnectionInfo> Readers { get; }

    bool IsStarting { get; }

    // Ciclo de vida
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);

    // Operaciones individuales
    Task ConnectAsync(int readerId, CancellationToken ct = default);
    Task DisconnectAsync(int readerId, CancellationToken ct = default);

    // Comandos al lector
    void OpenRelay(int readerId);
    void Restart(int readerId);
    void EmergencyOpen();
    void EmergencyEnd();

    // CRUD — los cambios se detectan y propagan a otros puestos vía sync periódico
    Task<ReaderConnectionInfo> AddReaderAsync(Reader reader, CancellationToken ct = default);
    Task UpdateReaderAsync(Reader reader, CancellationToken ct = default);
    Task RemoveReaderAsync(int readerId, CancellationToken ct = default);

    // Eventos hacia el resto del sistema (fichajes, estados, etc.)
    event Action<ReaderConnectionInfo, LmpiEvent> EventReceived;
    event Action<ReaderConnectionInfo>             ConnectionStateChanged;
}
