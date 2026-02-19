using MySqlConnector;
using System.Data;

namespace CardPass3.WPF.Services.Database;

/// <summary>
/// Provides MySQL connections. Connection string is loaded from appsettings.json.
/// </summary>
public interface IDatabaseConnectionFactory
{
    IDbConnection CreateConnection();
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default);
}

public class MySqlConnectionFactory : IDatabaseConnectionFactory
{
    private readonly string _connectionString;

    public MySqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("CardPass3")
            ?? throw new InvalidOperationException("Connection string 'CardPass3' not found in configuration.");
    }

    public IDbConnection CreateConnection()
        => new MySqlConnection(_connectionString);

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
