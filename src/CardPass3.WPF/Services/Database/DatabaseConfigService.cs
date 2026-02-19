using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;

namespace CardPass3.WPF.Services.Database;

public interface IDatabaseConfigService
{
    DatabaseConfig Config { get; }
    string GetConnectionString();
    string GetServerConnectionString();
    void Save(DatabaseConfig config, string plainPassword);
    bool Exists { get; }
    /// <summary>
    /// True si la contraseña almacenada no se pudo descifrar (formato antiguo o corrupto).
    /// El login lo usa para ofrecer ir a reconfigurar la BD en lugar de mostrar un error genérico.
    /// </summary>
    bool IsPasswordCorrupted { get; }
    void GenerateDefault();
    void ResetToDefault();
    bool TestConnection(DatabaseConfig config, string plainPassword);
}

public class DatabaseConfigService : IDatabaseConfigService
{
    private static readonly string ConfigFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "CardPass3", "Database");

    private static readonly string ConfigFile = Path.Combine(ConfigFolder, "cp3db.config.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly ILogger<DatabaseConfigService> _logger;
    private DatabaseConfig? _cache;

    public bool IsPasswordCorrupted { get; private set; }

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
        => Config.ToConnectionString(TryDecryptPassword(Config.DbPassword));

    public string GetServerConnectionString()
        => Config.ToServerConnectionString(TryDecryptPassword(Config.DbPassword));

    public void Save(DatabaseConfig config, string plainPassword)
    {
        config.DbPassword = string.IsNullOrWhiteSpace(plainPassword)
            ? string.Empty
            : DatabaseEncryption.Encrypt(plainPassword);

        if (!Directory.Exists(ConfigFolder))
            Directory.CreateDirectory(ConfigFolder);

        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ConfigFile, json);

        _cache = config;
        IsPasswordCorrupted = false;   // al guardar la contraseña ya está en formato correcto
        _logger.LogInformation("Configuración de BD guardada en {Path}", ConfigFile);
    }

    public void GenerateDefault()
    {
        _logger.LogInformation("Generando configuración de BD por defecto en {Path}", ConfigFile);
        Save(new DatabaseConfig(), plainPassword: "cardpass3");
    }

    /// <summary>
    /// Restablece el fichero de configuración a los valores de instalación por defecto,
    /// cifrando la contraseña "cardpass3" con el algoritmo actual.
    /// </summary>
    public void ResetToDefault()
    {
        _logger.LogInformation("Restableciendo configuración de BD a valores por defecto en {Path}", ConfigFile);
        IsPasswordCorrupted = false;
        Save(new DatabaseConfig(), plainPassword: "cardpass3");
    }

    public bool TestConnection(DatabaseConfig config, string plainPassword)
    {
        try
        {
            using var conn = new MySqlConnector.MySqlConnection(config.ToConnectionString(plainPassword));
            conn.Open();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Test de conexión fallido: {Error}", ex.Message);
            return false;
        }
    }

    // ── Privado ───────────────────────────────────────────────────────────────

    private void Load()
    {
        if (!File.Exists(ConfigFile))
        {
            _logger.LogWarning("Fichero de config de BD no encontrado. Generando valores por defecto en {Path}", ConfigFile);
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

    /// <summary>
    /// Intenta descifrar la contraseña. Si falla (formato antiguo, corrupto, etc.)
    /// devuelve string.Empty y activa IsPasswordCorrupted en lugar de propagar la excepción.
    /// </summary>
    private string TryDecryptPassword(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
            return string.Empty;

        try
        {
            return DatabaseEncryption.Decrypt(stored);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Contraseña de BD en formato incompatible (versión anterior o corrupta): {Error}.",
                ex.Message);
            IsPasswordCorrupted = true;
            return string.Empty;
        }
    }
}
