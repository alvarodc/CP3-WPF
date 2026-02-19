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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;
using System.Windows;

namespace CardPass3.WPF
{
    public partial class App : Application
    {
        private IHost? _host;

        public static IServiceProvider Services => ((App)Current)._host!.Services;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/cardpass3-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
                .CreateLogger();

            _host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureAppConfiguration(cfg => cfg
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false))
                .ConfigureServices(ConfigureServices)
                .Build();

            await _host.StartAsync();

            var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IDatabaseConnectionFactory, MySqlConnectionFactory>();

            services.AddSingleton<IOperatorRepository, OperatorRepository>();
            services.AddSingleton<IReaderRepository, ReaderRepository>();
            services.AddSingleton<IEventRepository, EventRepository>();
            services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();

            services.AddSingleton<IReaderDriverFactory, DefaultReaderDriverFactory>();
            services.AddSingleton<IReaderConnectionService, ReaderConnectionService>();
            services.AddSingleton<INavigationService, NavigationService>();

            services.AddTransient<LoginViewModel>();
            services.AddSingleton<ShellViewModel>();
            services.AddSingleton<ReadersViewModel>();

            services.AddTransient<LoginWindow>();
            services.AddSingleton<ShellWindow>();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host is not null)
            {
                var readerService = _host.Services.GetRequiredService<IReaderConnectionService>();
                await readerService.DisconnectAllAsync();
                await _host.StopAsync();
                _host.Dispose();
            }
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
