using CardPass3.WPF.Services.Database;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CardPass3.WPF.Modules.Configuration.ViewModels
{
    public partial class DatabaseConfigViewModel : ObservableObject
    {
        private readonly IDatabaseConfigService _configService;
        private readonly ILogger<DatabaseConfigViewModel> _logger;

        [ObservableProperty] private string _dbHost     = string.Empty;
        [ObservableProperty] private string _dbPort     = string.Empty;
        [ObservableProperty] private string _dbUser     = string.Empty;
        [ObservableProperty] private string _dbName     = string.Empty;

        // La contraseña no se pre-rellena por seguridad — el usuario debe introducirla para guardar
        [ObservableProperty] private string _dbPassword = string.Empty;

        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private bool   _statusIsError;
        [ObservableProperty] private bool   _isBusy;

        public DatabaseConfigViewModel(
            IDatabaseConfigService configService,
            ILogger<DatabaseConfigViewModel> logger)
        {
            _configService = configService;
            _logger = logger;
            LoadCurrent();
        }

        private void LoadCurrent()
        {
            var cfg = _configService.Config;
            DbHost = cfg.DbHost;
            DbPort = cfg.DbPort;
            DbUser = cfg.DbUser;
            DbName = cfg.DbName;
            // No cargamos la contraseña — siempre hay que reintroducirla para guardar
        }

        [RelayCommand]
        private async Task TestConnectionAsync()
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            await Task.Run(() =>
            {
                try
                {
                    var testConfig = BuildConfig();
                    var ok = _configService.TestConnection(testConfig, DbPassword);
                    StatusIsError  = !ok;
                    StatusMessage  = ok
                        ? "✓ Conexión exitosa"
                        : "✗ No se pudo conectar. Comprueba los parámetros.";
                }
                catch (Exception ex)
                {
                    StatusIsError = true;
                    StatusMessage = $"✗ Error: {ex.Message}";
                    _logger.LogWarning(ex, "Test de conexión fallido");
                }
            });

            IsBusy = false;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            await Task.Run(() =>
            {
                try
                {
                    var config = BuildConfig();

                    // Verificar conexión antes de guardar
                    if (!_configService.TestConnection(config, DbPassword))
                    {
                        StatusIsError = true;
                        StatusMessage = "✗ No se guardó: la conexión falló. Verifica los parámetros.";
                        return;
                    }

                    _configService.Save(config, DbPassword);
                    StatusIsError = false;
                    StatusMessage = "✓ Configuración guardada correctamente. Reinicia la aplicación para aplicar los cambios.";
                    _logger.LogInformation("Configuración de BD actualizada por el usuario.");
                }
                catch (Exception ex)
                {
                    StatusIsError = true;
                    StatusMessage = $"✗ Error al guardar: {ex.Message}";
                    _logger.LogError(ex, "Error al guardar configuración de BD");
                }
            });

            IsBusy = false;
        }

        private DatabaseConfig BuildConfig() => new()
        {
            DbHost = DbHost.Trim(),
            DbPort = DbPort.Trim(),
            DbUser = DbUser.Trim(),
            DbName = DbName.Trim()
            // DbPassword lo gestiona el servicio al guardar
        };
    }
}
