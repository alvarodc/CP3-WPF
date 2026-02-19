namespace CardPass3.WPF.Services.Database;

/// <summary>
/// Parámetros de conexión a la base de datos.
/// Serializado a JSON en %ProgramData%\CardPass3\Database\cp3db.config.json
/// La contraseña se almacena cifrada con AES-256-GCM.
/// </summary>
public class DatabaseConfig
{
    public string DbHost     { get; set; } = "localhost";
    public string DbPort     { get; set; } = "3306";
    public string DbUser     { get; set; } = "cardpass3";
    /// <summary>Contraseña cifrada. Nunca en texto plano.</summary>
    public string DbPassword { get; set; } = string.Empty;
    public string DbName     { get; set; } = "cardpass3";

    public string ToConnectionString(string plainPassword)
        => $"Server={DbHost};Port={DbPort};Database={DbName};User={DbUser};" +
           $"Password={plainPassword};" +
           $"AllowUserVariables=True;UseAffectedRows=False;CharSet=utf8;";

    public string ToServerConnectionString(string plainPassword)
        => $"Server={DbHost};Port={DbPort};User={DbUser};" +
           $"Password={plainPassword};" +
           $"AllowUserVariables=True;UseAffectedRows=False;";
}
