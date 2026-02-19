using CardPass3.WPF.Data.Repositories;
using CardPass3.WPF.Data.Repositories.Interfaces;
using CardPass3.WPF.Modules.Login.ViewModels;
using CardPass3.WPF.Modules.Login.Views;
using CardPass3.WPF.Modules.Shell.ViewModels;
using CardPass3.WPF.Modules.Shell.Views;
using CardPass3.WPF.Services.Database;
using CardPass3.WPF.Services.Navigation;
using CardPass3.WPF.Services.Readers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Windows;

namespace CardPass3.WPF;

public partial class App : Application
{
    private IHost? _host;

    public static IServiceProvider Services => ((App)Current)._host!.Services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/cardpass3-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(ConfigureServices)
            .Build();

        await _host.StartAsync();

        // Show login window
        var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
        loginWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // --- Infrastructure ---
        services.AddSingleton<IDatabaseConnectionFactory, MySqlConnectionFactory>();

        // --- Repositories ---
        services.AddSingleton<IOperatorRepository, OperatorRepository>();
        services.AddSingleton<IReaderRepository, ReaderRepository>();
        services.AddSingleton<IEventRepository, EventRepository>();
        services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();

        // --- Services ---
        services.AddSingleton<IReaderConnectionService, ReaderConnectionService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // --- ViewModels ---
        services.AddTransient<LoginViewModel>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<ReadersViewModel>();

        // --- Views (Windows) ---
        services.AddTransient<LoginWindow>();
        services.AddSingleton<ShellWindow>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            // Gracefully disconnect all readers on exit
            var readerService = _host.Services.GetRequiredService<IReaderConnectionService>();
            await readerService.DisconnectAllAsync();

            await _host.StopAsync();
            _host.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
