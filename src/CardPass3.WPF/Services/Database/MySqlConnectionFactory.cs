using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.Data;

namespace CardPass3.WPF.Services.Database;

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
            ?? throw new InvalidOperationException("Connection string 'CardPass3' not found.");
    }

    public IDbConnection CreateConnection() => new MySqlConnection(_connectionString);

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
    {
        var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }
}
