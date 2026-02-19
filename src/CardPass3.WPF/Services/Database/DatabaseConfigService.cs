using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;

namespace CardPass3.WPF.Services.Database
{
    /// <summary>
    /// Gestiona la configuración de conexión a la base de datos.
    /// 
    /// Ubicación del fichero:
    ///   %ProgramData%\CardPass3\Database\cp3db.config.json
    ///   → C:\ProgramData\CardPass3\Database\cp3db.config.json
    /// 
    /// La contraseña se almacena cifrada (AES-256-GCM).
    /// El fichero se crea automáticamente con valores por defecto si no existe.
    /// </summary>
    public interface IDatabaseConfigService
    {
        DatabaseConfig Config { get; }
        string GetConnectionString();
        string GetServerConnectionString();
        void Save(DatabaseConfig config, string plainPassword);
        bool Exists { get; }
        void GenerateDefault();
        bool TestConnection(DatabaseConfig config, string plainPassword);
    }

    public class DatabaseConfigService : IDatabaseConfigService
    {
        private static readonly string ConfigFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "CardPass3", "Database");

        private static readonly string ConfigFile = Path.Combine(ConfigFolder, "cp3db.config.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        private readonly ILogger<DatabaseConfigService> _logger;
        private DatabaseConfig? _cache;

        public DatabaseConfigService(ILogger<DatabaseConfigService> logger)
        {
            _logger = logger;
        }

        public bool Exists => File.Exists(ConfigFile);

        public DatabaseConfig Config
        {
            get
            {
                if (_cache is null) Load();
                return _cache!;
            }
        }

        public string GetConnectionString()
        {
            var cfg = Config;
            var plain = string.IsNullOrWhiteSpace(cfg.DbPassword)
                ? string.Empty
                : DatabaseEncryption.Decrypt(cfg.DbPassword);
            return cfg.ToConnectionString(plain);
        }

        public string GetServerConnectionString()
        {
            var cfg = Config;
            var plain = string.IsNullOrWhiteSpace(cfg.DbPassword)
                ? string.Empty
                : DatabaseEncryption.Decrypt(cfg.DbPassword);
            return cfg.ToServerConnectionString(plain);
        }

        public void Save(DatabaseConfig config, string plainPassword)
        {
            // Cifrar la contraseña antes de guardar
            config.DbPassword = string.IsNullOrWhiteSpace(plainPassword)
                ? string.Empty
                : DatabaseEncryption.Encrypt(plainPassword);

            if (!Directory.Exists(ConfigFolder))
                Directory.CreateDirectory(ConfigFolder);

            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(ConfigFile, json);

            _cache = config;
            _logger.LogInformation("Configuración de BD guardada en {Path}", ConfigFile);
        }

        public void GenerateDefault()
        {
            _logger.LogInformation("Generando configuración de BD por defecto en {Path}", ConfigFile);
            Save(new DatabaseConfig(), plainPassword: string.Empty);
        }

        public bool TestConnection(DatabaseConfig config, string plainPassword)
        {
            var connStr = config.ToConnectionString(plainPassword);
            try
            {
                using var conn = new MySqlConnector.MySqlConnection(connStr);
                conn.Open();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Test de conexión fallido: {Error}", ex.Message);
                return false;
            }
        }

        // ── Privado ──────────────────────────────────────────────────────────

        private void Load()
        {
            if (!File.Exists(ConfigFile))
            {
                _logger.LogWarning("Fichero de configuración de BD no encontrado: {Path}. Generando valores por defecto.", ConfigFile);
                GenerateDefault();
                return;
            }

            try
            {
                var json = File.ReadAllText(ConfigFile);
                _cache = JsonSerializer.Deserialize<DatabaseConfig>(json, JsonOptions)
                         ?? throw new InvalidOperationException("El fichero de configuración de BD está vacío o corrupto.");

                _logger.LogInformation("Configuración de BD cargada desde {Path}", ConfigFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al leer el fichero de configuración de BD: {Path}", ConfigFile);
                throw;
            }
        }
    }
}
