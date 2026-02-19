using CardPass3.WPF.Data.Models;
using CardPass3.WPF.Data.Repositories.Interfaces;
using CardPass3.WPF.Services.Readers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CardPass3.WPF.Modules.Readers.ViewModels;

public partial class ReadersViewModel : ObservableObject
{
    private readonly IReaderConnectionService _connectionService;
    private readonly IReaderRepository _readerRepo;

    public ReadOnlyObservableCollection<ReaderConnectionInfo> Readers
        => _connectionService.Readers;

    [ObservableProperty]
    private ReaderConnectionInfo? _selectedReader;

    [ObservableProperty]
    private bool _isBusy;

    public ReadersViewModel(IReaderConnectionService connectionService, IReaderRepository readerRepo)
    {
        _connectionService = connectionService;
        _readerRepo = readerRepo;
    }

    [RelayCommand(CanExecute = nameof(CanActOnReader))]
    private async Task ConnectReaderAsync()
    {
        if (SelectedReader is null) return;
        IsBusy = true;
        try { await _connectionService.ConnectReaderAsync(SelectedReader.Reader.IdReader); }
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanActOnReader))]
    private async Task DisconnectReaderAsync()
    {
        if (SelectedReader is null) return;
        IsBusy = true;
        try { await _connectionService.DisconnectReaderAsync(SelectedReader.Reader.IdReader); }
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanActOnReader))]
    private async Task OpenRelayAsync()
    {
        if (SelectedReader is null) return;
        // Driver call goes through connection service / driver factory
        // TODO: expose OpenRelay on IReaderConnectionService or resolve driver directly
        await Task.CompletedTask;
    }

    private bool CanActOnReader() => SelectedReader is not null && !IsBusy;
}
