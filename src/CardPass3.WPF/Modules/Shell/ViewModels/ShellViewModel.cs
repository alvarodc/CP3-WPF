using CardPass3.WPF.Services.Navigation;
using CardPass3.WPF.Services.Readers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CardPass3.WPF.Modules.Shell.ViewModels
{

public partial class ShellViewModel : ObservableObject
{
    private readonly IReaderConnectionService _readerService;
    private readonly INavigationService _navigation;

    // Expose reader list directly from the service — no copy needed
    public ObservableCollection<ReaderConnectionInfo> Readers
        => _readerService.Readers;

    [ObservableProperty]
    private bool _readersLoading;

    [ObservableProperty]
    private string _readersStatus = "Conectando lectores...";

    [ObservableProperty]
    private string _currentModuleTitle = "CardPass3";

    [ObservableProperty]
    private string _currentOperatorName = string.Empty;

    [ObservableProperty]
    private object? _currentView;

    public ShellViewModel(IReaderConnectionService readerService, INavigationService navigation)
    {
        _readerService = readerService;
        _navigation = navigation;

        // Watch the connecting flag to update status bar
        // ReaderConnectionService updates IsStarting while the initial sweep runs
        MonitorReadersAsync();
    }

    private async void MonitorReadersAsync()
    {
        ReadersLoading = true;

        // Suscribir ANTES del bucle para no perder eventos si el servicio
        // termina de arrancar justo mientras esperamos.
        _readerService.ConnectionStateChanged += _ => UpdateReadersStatus();

        while (_readerService.IsStarting)
            await Task.Delay(200);

        UpdateReadersStatus();
        ReadersLoading = false;
    }

    private void UpdateReadersStatus()
    {
        // El evento ConnectionStateChanged puede venir de un hilo de red — marshalizar al UI thread.
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            var connected = _readerService.Readers.Count(r => r.State == ReaderConnectionState.ReaderConnected);
            var total     = _readerService.Readers.Count;
            ReadersStatus = $"Lectores: {connected}/{total} conectados";
        });
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

    /// <summary>Raised when the user requests logout. ShellWindow subscribes and handles the window transition.</summary>
    public event EventHandler? LogoutRequested;

    [RelayCommand]
    private void Logout() => LogoutRequested?.Invoke(this, EventArgs.Empty);
}
}
