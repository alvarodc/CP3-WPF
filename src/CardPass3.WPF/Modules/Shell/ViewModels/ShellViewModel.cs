using CardPass3.WPF.Services.Navigation;
using CardPass3.WPF.Services.Readers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace CardPass3.WPF.Modules.Shell.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly IReaderConnectionService _readerService;
    private readonly INavigationService       _navigation;

    // ObservableCollection directa del servicio — WPF detecta Add/Remove automáticamente
    public ObservableCollection<ReaderConnectionInfo> Readers => _readerService.Readers;

    [ObservableProperty] private bool   _readersLoading;
    [ObservableProperty] private string _readersStatus       = "Conectando lectores...";
    [ObservableProperty] private string _currentModuleTitle  = "CardPass3";
    [ObservableProperty] private string _currentOperatorName = string.Empty;
    [ObservableProperty] private object? _currentView;

    public ShellViewModel(IReaderConnectionService readerService, INavigationService navigation)
    {
        _readerService = readerService;
        _navigation    = navigation;

        // Actualizar contadores cuando la colección cambia (Add/Remove durante startup)
        _readerService.Readers.CollectionChanged += OnReadersCollectionChanged;

        // Actualizar contadores cuando cambia el estado de conexión de un lector
        _readerService.ConnectionStateChanged += _ => UpdateReadersStatus();

        MonitorStartupAsync();
    }

    private void OnReadersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => UpdateReadersStatus();

    private async void MonitorStartupAsync()
    {
        ReadersLoading = true;
        while (_readerService.IsStarting)
            await Task.Delay(300);
        UpdateReadersStatus();
        ReadersLoading = false;
    }

    private void UpdateReadersStatus()
    {
        var connected = _readerService.Readers.Count(r => r.State == ReaderConnectionState.ReaderConnected);
        var total     = _readerService.Readers.Count;
        ReadersStatus = $"Lectores: {connected}/{total} conectados";
    }

    [RelayCommand]
    private void Navigate(string module)
    {
        CurrentModuleTitle = module switch
        {
            "pass"    => "Control de paso",
            "readers" => "Lectores",
            "users"   => "Usuarios",
            "events"  => "Fichajes",
            "areas"   => "Áreas",
            "config"  => "Configuración",
            _         => "CardPass3"
        };
        CurrentView = _navigation.Resolve(module);
    }

    public event EventHandler? LogoutRequested;

    [RelayCommand]
    private void Logout() => LogoutRequested?.Invoke(this, EventArgs.Empty);
}
