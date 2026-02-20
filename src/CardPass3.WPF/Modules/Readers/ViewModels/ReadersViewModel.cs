using CardPass3.WPF.Services.Readers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CardPass3.WPF.Modules.Readers.ViewModels;

public partial class ReadersViewModel : ObservableObject
{
    private readonly IReaderConnectionService _connectionService;

    public IReadOnlyList<ReaderConnectionInfo> Readers => _connectionService.Readers;

    public int ConnectedCount
        => _connectionService.Readers.Count(r => r.State == ReaderConnectionState.ReaderConnected);
    public int TotalCount
        => _connectionService.Readers.Count;

    [ObservableProperty]
    private ReaderConnectionInfo? _selectedReader;

    [ObservableProperty]
    private bool _isBusy;

    public ReadersViewModel(IReaderConnectionService connectionService)
    {
        _connectionService = connectionService;

        // Refrescar contadores cuando cambia el estado de cualquier lector
        _connectionService.ConnectionStateChanged += _ =>
        {
            OnPropertyChanged(nameof(ConnectedCount));
            OnPropertyChanged(nameof(TotalCount));
        };
    }

    [RelayCommand(CanExecute = nameof(CanActOnReader))]
    private async Task ConnectReaderAsync()
    {
        if (SelectedReader is null) return;
        IsBusy = true;
        try   { await _connectionService.ConnectAsync(SelectedReader.Reader.IdReader); }
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanActOnReader))]
    private async Task DisconnectReaderAsync()
    {
        if (SelectedReader is null) return;
        IsBusy = true;
        try   { await _connectionService.DisconnectAsync(SelectedReader.Reader.IdReader); }
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanActOnConnectedReader))]
    private void OpenRelay()
    {
        if (SelectedReader is null) return;
        _connectionService.OpenRelay(SelectedReader.Reader.IdReader);
    }

    [RelayCommand(CanExecute = nameof(CanActOnConnectedReader))]
    private void RestartReader()
    {
        if (SelectedReader is null) return;
        _connectionService.Restart(SelectedReader.Reader.IdReader);
    }

    [RelayCommand]
    private void EmergencyOpen() => _connectionService.EmergencyOpen();

    [RelayCommand]
    private void EmergencyEnd() => _connectionService.EmergencyEnd();

    private bool CanActOnReader()          => SelectedReader is not null && !IsBusy;
    private bool CanActOnConnectedReader() => SelectedReader?.IsOperational == true && !IsBusy;
}
