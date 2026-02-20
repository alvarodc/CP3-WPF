using CardPass3.WPF.Data.Models;
using CardPass3.WPF.Data.Repositories.Interfaces;
using CardPass3.WPF.Modules.Readers.Views;
using CardPass3.WPF.Services.Readers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;

namespace CardPass3.WPF.Modules.Readers.ViewModels;

public partial class ReadersViewModel : ObservableObject
{
    private readonly IReaderConnectionService _connectionService;
    private readonly IAreaRepository          _areaRepo;

    // ObservableCollection directa del servicio — DataGrid se refresca solo
    public ObservableCollection<ReaderConnectionInfo> Readers => _connectionService.Readers;

    public int ConnectedCount
        => _connectionService.Readers.Count(r => r.State == ReaderConnectionState.ReaderConnected);
    public int TotalCount
        => _connectionService.Readers.Count;

    [ObservableProperty] private ReaderConnectionInfo? _selectedReader;
    [ObservableProperty] private bool _isBusy;

    public ReadersViewModel(IReaderConnectionService connectionService, IAreaRepository areaRepo)
    {
        _connectionService = connectionService;
        _areaRepo          = areaRepo;

        _connectionService.Readers.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(ConnectedCount));
            OnPropertyChanged(nameof(TotalCount));
        };
        _connectionService.ConnectionStateChanged += _ =>
        {
            OnPropertyChanged(nameof(ConnectedCount));
        };
    }

    // ── Conexión ──────────────────────────────────────────────────────────────

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

    // ── Comandos al lector ────────────────────────────────────────────────────

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

    [RelayCommand] private void EmergencyOpen() => _connectionService.EmergencyOpen();
    [RelayCommand] private void EmergencyEnd()  => _connectionService.EmergencyEnd();

    // ── Habilitar / deshabilitar ──────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanActOnDisabledReader))]
    private async Task EnableReaderAsync()
    {
        if (SelectedReader is null) return;
        IsBusy = true;
        try   { await _connectionService.SetEnabledAsync(SelectedReader.Reader.IdReader, true); }
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanActOnEnabledReader))]
    private async Task DisableReaderAsync()
    {
        if (SelectedReader is null) return;
        IsBusy = true;
        try   { await _connectionService.SetEnabledAsync(SelectedReader.Reader.IdReader, false); }
        finally { IsBusy = false; }
    }

    // ── CRUD ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task AddReaderAsync()
    {
        var vm  = new ReaderFormViewModel(_areaRepo);
        var win = new ReaderFormWindow(vm) { Owner = Application.Current.MainWindow };

        if (win.ShowDialog() != true || vm.ResultReader is null) return;

        IsBusy = true;
        try   { await _connectionService.AddReaderAsync(vm.ResultReader); }
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanActOnReader))]
    private async Task EditReaderAsync()
    {
        if (SelectedReader is null) return;

        var vm  = new ReaderFormViewModel(_areaRepo, SelectedReader.Reader);
        var win = new ReaderFormWindow(vm) { Owner = Application.Current.MainWindow };

        if (win.ShowDialog() != true || vm.ResultReader is null) return;

        IsBusy = true;
        try   { await _connectionService.UpdateReaderAsync(vm.ResultReader); }
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanActOnReader))]
    private async Task DeleteReaderAsync()
    {
        if (SelectedReader is null) return;

        var name   = SelectedReader.Reader.ReaderDescription;
        var result = MessageBox.Show(
            $"¿Eliminar el lector «{name}»?\n\nSe desconectará y se marcará como eliminado.",
            "Confirmar eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        IsBusy = true;
        try   { await _connectionService.RemoveReaderAsync(SelectedReader.Reader.IdReader); }
        finally { IsBusy = false; }
    }

    // ── CanExecute ────────────────────────────────────────────────────────────

    private bool CanActOnReader()          => SelectedReader is not null && !IsBusy;
    private bool CanActOnConnectedReader() => SelectedReader?.IsOperational == true && !IsBusy;
    private bool CanActOnEnabledReader()   => SelectedReader?.Reader.Enabled == true && !IsBusy;
    private bool CanActOnDisabledReader()  => SelectedReader?.Reader.Enabled == false && !IsBusy;
}
