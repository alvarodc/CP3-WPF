using CardPass3.WPF.Data.Repositories;
using CardPass3.WPF.Data.Repositories.Interfaces;
using CardPass3.WPF.Modules.Login.ViewModels;
using CardPass3.WPF.Modules.Login.Views;
using CardPass3.WPF.Modules.Readers.ViewModels;
using CardPass3.WPF.Modules.Shell.ViewModels;
using CardPass3.WPF.Modules.Shell.Views;
using CardPass3.WPF.Services.Database;
using CardPass3.WPF.Services.Navigation;
using CardPass3.WPF.Services.Readers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

        // ── Captura de excepciones no manejadas ───────────────────────────────
        DispatcherUnhandledException += (s, ex) =>
        {
            Log.Error(ex.Exception, "Excepción no manejada en el hilo de UI");
            MessageBox.Show(
                $"Se ha producido un error inesperado:\n\n{ex.Exception.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true; // Evita que la app se cierre
        };

        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            var exception = ex.ExceptionObject as Exception;
            Log.Fatal(exception, "Excepción crítica no manejada (AppDomain)");
            MessageBox.Show(
                $"Error crítico:\n\n{exception?.Message ?? ex.ExceptionObject.ToString()}",
                "Error crítico", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        TaskScheduler.UnobservedTaskException += (s, ex) =>
        {
            Log.Warning(ex.Exception, "Excepción no observada en Task");
            ex.SetObserved(); // Evita que tumbe el proceso
        };

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/cardpass3-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(ConfigureServices)
            .Build();

        await _host.StartAsync();

        // Generar config de BD en primer arranque
        var dbConfig = _host.Services.GetRequiredService<IDatabaseConfigService>();
        if (!dbConfig.Exists)
        {
            dbConfig.GenerateDefault();
            Log.Information("Primer arranque: config de BD generada en ProgramData.");
        }

        // Arrancar lectores en background y esperar a que termine antes de iniciar el sync.
        // Si el sync arranca antes de que StartAsync haya poblado _readers, se producen
        // duplicados en la colección y ArgumentException en el ToDictionary del diff.
        var readerService = _host.Services.GetRequiredService<IReaderConnectionService>();
        var syncService   = _host.Services.GetRequiredService<ReaderSyncService>();

        _ = Task.Run(async () =>
        {
            await readerService.StartAsync();
            syncService.Start();   // Solo arranca una vez que la carga inicial ha terminado
        });

        var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
        loginWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // ── Database ──────────────────────────────────────────────────────────
        services.AddSingleton<IDatabaseConfigService, DatabaseConfigService>();
        services.AddSingleton<IDatabaseConnectionFactory, MySqlConnectionFactory>();

        // ── Repositories ──────────────────────────────────────────────────────
        services.AddSingleton<IOperatorRepository, OperatorRepository>();
        services.AddSingleton<IReaderRepository, ReaderRepository>();
        services.AddSingleton<IAreaRepository, AreaRepository>();
        services.AddSingleton<IEventRepository, EventRepository>();
        services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();

        // ── Reader services ───────────────────────────────────────────────────
        // ReaderConnectionService se registra también como su tipo concreto
        // para que ReaderSyncService pueda acceder a ApplySyncDiffAsync (internal)
        services.AddSingleton<ReaderConnectionService>();
        services.AddSingleton<IReaderConnectionService>(sp =>
            sp.GetRequiredService<ReaderConnectionService>());
        services.AddSingleton<ReaderSyncService>();

        // ── Navigation ────────────────────────────────────────────────────────
        services.AddSingleton<INavigationService, NavigationService>();

        // ── ViewModels ────────────────────────────────────────────────────────
        services.AddTransient<LoginViewModel>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<ReadersViewModel>();

        // ── Views ─────────────────────────────────────────────────────────────
        services.AddTransient<LoginWindow>();
        services.AddSingleton<ShellWindow>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            // Parar sync y desconectar lectores limpiamente
            var syncService = _host.Services.GetRequiredService<ReaderSyncService>();
            var readerService = _host.Services.GetRequiredService<IReaderConnectionService>();

            await syncService.StopAsync();
            await readerService.StopAsync();
            await _host.StopAsync();
            _host.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
