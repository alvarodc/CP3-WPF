using MySqlConnector;
using System.Data;

namespace CardPass3.WPF.Services.Database
{
    public interface IDatabaseConnectionFactory
    {
        IDbConnection CreateConnection();
        Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default);
    }

    public class MySqlConnectionFactory : IDatabaseConnectionFactory
    {
        private readonly IDatabaseConfigService _configService;

        public MySqlConnectionFactory(IDatabaseConfigService configService)
        {
            _configService = configService;
        }

        public IDbConnection CreateConnection()
            => new MySqlConnection(_configService.GetConnectionString());

        public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
        {
            var conn = new MySqlConnection(_configService.GetConnectionString());
            await conn.OpenAsync(ct);
            return conn;
        }
    }
}
