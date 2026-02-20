using CardPass3.WPF.Services.Navigation;
using CardPass3.WPF.Services.Readers;
using System.Collections.Specialized;
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
        _navigation    = navigation;

        // Escuchar cambios en la colección (altas/bajas de lectores)
        _readerService.Readers.CollectionChanged += OnReadersCollectionChanged;

        // Suscribir a los lectores ya cargados al construirse el ViewModel
        foreach (var info in _readerService.Readers)
            info.PropertyChanged += OnReaderInfoPropertyChanged;

        MonitorReadersAsync();
    }

    private void OnReadersCollectionChanged(object? sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (ReaderConnectionInfo info in e.NewItems)
                info.PropertyChanged += OnReaderInfoPropertyChanged;

        if (e.OldItems is not null)
            foreach (ReaderConnectionInfo info in e.OldItems)
                info.PropertyChanged -= OnReaderInfoPropertyChanged;

        UpdateReadersStatus();
    }

    private void OnReaderInfoPropertyChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ReaderConnectionInfo.State))
            UpdateReadersStatus();
    }

    private async void MonitorReadersAsync()
    {
        ReadersLoading = true;

        while (_readerService.IsStarting)
            await Task.Delay(200);

        UpdateReadersStatus();
        ReadersLoading = false;
    }

    private void UpdateReadersStatus()
    {
        // Ya estamos en el UI thread porque ReaderConnectionService despacha
        // los cambios de estado con OnUiThread(). UpdateReadersStatus puede
        // llamarse también desde CollectionChanged (UI thread), así que es seguro.
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

    /// <summary>Raised when the user requests logout. ShellWindow subscribes and handles the window transition.</summary>
    public event EventHandler? LogoutRequested;

    [RelayCommand]
    private void Logout() => LogoutRequested?.Invoke(this, EventArgs.Empty);
}
}
