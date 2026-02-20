using CardPass3.WPF.Data.Models;
using CardPass3.WPF.Services.Readers.Lmpi;
using System.Collections.ObjectModel;

namespace CardPass3.WPF.Services.Readers;

public enum ReaderConnectionState
{
    Idle,
    Connecting,
    TcpConnected,
    ReaderConnected,
    Failed,
    Disconnected
}

public sealed class ReaderConnectionInfo
{
    public required Reader Reader      { get; set; }
    public ReaderConnectionState State { get; set; } = ReaderConnectionState.Idle;
    public AppState    AppState        { get; set; } = AppState.Control;
    public ReaderState ReaderState     { get; set; } = ReaderState.Control;
    public string?     ErrorMessage    { get; set; }
    public DateTime?   ConnectedAt     { get; set; }
    public DateTime?   LastAttemptAt   { get; set; }

    public bool IsOperational => State == ReaderConnectionState.ReaderConnected;
    public bool IsEmergency   => AppState == AppState.Emergency || ReaderState == ReaderState.Emergency;
}

public interface IReaderConnectionService
{
    ObservableCollection<ReaderConnectionInfo> Readers { get; }
    bool IsStarting { get; }

    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);

    Task ConnectAsync(int readerId, CancellationToken ct = default);
    Task DisconnectAsync(int readerId, CancellationToken ct = default);
    Task SetEnabledAsync(int readerId, bool enabled, CancellationToken ct = default);

    void OpenRelay(int readerId);
    void Restart(int readerId);
    void EmergencyOpen();
    void EmergencyEnd();

    Task<ReaderConnectionInfo> AddReaderAsync(Reader reader, CancellationToken ct = default);
    Task UpdateReaderAsync(Reader reader, CancellationToken ct = default);
    Task RemoveReaderAsync(int readerId, CancellationToken ct = default);

    event Action<ReaderConnectionInfo, LmpiEvent> EventReceived;
    event Action<ReaderConnectionInfo>             ConnectionStateChanged;
}
