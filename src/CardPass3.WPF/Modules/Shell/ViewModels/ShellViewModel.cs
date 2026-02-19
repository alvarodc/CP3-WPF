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
    public ReadOnlyObservableCollection<ReaderConnectionInfo> Readers
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
        // ReaderConnectionService updates IsConnecting when sweep finishes
        MonitorReadersAsync();
    }

    private async void MonitorReadersAsync()
    {
        ReadersLoading = true;

        // Poll until sweep completes (simple approach; can be replaced with event/Task)
        while (_readerService.IsConnecting)
            await Task.Delay(500);

        var connected = Readers.Count(r => r.State == ReaderConnectionState.Connected);
        var total = Readers.Count;
        ReadersStatus = $"Lectores: {connected}/{total} conectados";
        ReadersLoading = false;
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
