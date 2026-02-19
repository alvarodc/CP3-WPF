using CardPass3.WPF.Modules.Login.Views;
using CardPass3.WPF.Modules.Shell.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Threading;

namespace CardPass3.WPF.Modules.Shell.Views;

public partial class ShellWindow : Window
{
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };

    public ShellWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Logout: ViewModel raises event, View handles window transition
        viewModel.LogoutRequested += OnLogoutRequested;

        // Live clock in topbar
        _clock.Tick += (_, _) => TxtClock.Text = DateTime.Now.ToString("ddd dd/MM/yyyy  HH:mm:ss");
        _clock.Start();
        TxtClock.Text = DateTime.Now.ToString("ddd dd/MM/yyyy  HH:mm:ss");
    }

    private void OnLogoutRequested(object? sender, EventArgs e)
    {
        var login = App.Services.GetRequiredService<LoginWindow>();
        login.Show();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _clock.Stop();
        base.OnClosed(e);
        // Only shutdown if no other windows are open (e.g. login window may have been shown)
        if (Application.Current.Windows.Count == 0)
            Application.Current.Shutdown();
    }
}
