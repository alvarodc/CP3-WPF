using CardPass3.WPF.Data.Models;
using CardPass3.WPF.Services.Readers.Lmpi;
using CommunityToolkit.Mvvm.ComponentModel;
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

/// <summary>
/// Modelo observable que representa el estado en vivo de un lector.
/// Al heredar de ObservableObject, cada cambio de propiedad notifica
/// automáticamente al DataGrid y a cualquier binding de la UI sin
/// necesidad de recrear la colección ni hacer Invoke manual.
/// </summary>
public sealed partial class ReaderConnectionInfo : ObservableObject
{
    [ObservableProperty] private Reader _reader = null!;
    [ObservableProperty] private ReaderConnectionState _state = ReaderConnectionState.Idle;
    [ObservableProperty] private AppState    _appState    = AppState.Control;
    [ObservableProperty] private ReaderState _readerState = ReaderState.Control;
    [ObservableProperty] private string?     _errorMessage;
    [ObservableProperty] private DateTime?   _connectedAt;
    [ObservableProperty] private DateTime?   _lastAttemptAt;

    public bool IsOperational => State == ReaderConnectionState.ReaderConnected;
    public bool IsEmergency   => AppState == AppState.Emergency || ReaderState == ReaderState.Emergency;

    // Recalcular propiedades derivadas cuando cambia State, AppState o ReaderState
    partial void OnStateChanged(ReaderConnectionState value)
    {
        OnPropertyChanged(nameof(IsOperational));
        OnPropertyChanged(nameof(IsEmergency));
    }
    partial void OnAppStateChanged(AppState value)    => OnPropertyChanged(nameof(IsEmergency));
    partial void OnReaderStateChanged(ReaderState value) => OnPropertyChanged(nameof(IsEmergency));
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
