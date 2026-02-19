namespace CardPass3.WPF.Services.Database
{
    /// <summary>
    /// Parámetros de conexión a la base de datos.
    /// Se serializa a JSON en %ProgramData%\CardPass3\Database\cp3db.config.json
    /// La contraseña se almacena cifrada (AES-256).
    /// </summary>
    public class DatabaseConfig
    {
        public string DbHost     { get; set; } = "127.0.0.1";
        public string DbPort     { get; set; } = "3306";
        public string DbUser     { get; set; } = "root";
        /// <summary>Contraseña cifrada con AES-256. Nunca en texto plano.</summary>
        public string DbPassword { get; set; } = string.Empty;
        public string DbName     { get; set; } = "cardpass3";

        /// <summary>
        /// Construye la cadena de conexión MySqlConnector con los parámetros actuales.
        /// La contraseña se descifra en el momento de construir la cadena.
        /// </summary>
        public string ToConnectionString(string plainPassword)
            => $"Server={DbHost};Port={DbPort};Database={DbName};User={DbUser};" +
               $"Password={plainPassword};" +
               $"AllowUserVariables=True;UseAffectedRows=False;CharSet=utf8;";

        /// <summary>Cadena de conexión al servidor sin seleccionar base de datos (para restore).</summary>
        public string ToServerConnectionString(string plainPassword)
            => $"Server={DbHost};Port={DbPort};User={DbUser};" +
               $"Password={plainPassword};" +
               $"AllowUserVariables=True;UseAffectedRows=False;";
    }
}
