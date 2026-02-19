using CardPass3.WPF.Data.Models;
using CardPass3.WPF.Data.Repositories.Interfaces;
using CardPass3.WPF.Services.Database;
using Dapper;

namespace CardPass3.WPF.Data.Repositories;

public class OperatorRepository(IDatabaseConnectionFactory db) : IOperatorRepository
{
    public async Task<Operator?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id_operator        AS IdOperator,
                   operator_name      AS OperatorName,
                   password           AS Password,
                   operator_description AS OperatorDescription
            FROM operators
            WHERE operator_name = @name";

        using var conn = await db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Operator>(
            new CommandDefinition(sql, new { name }, cancellationToken: ct));
    }

    public async Task<IEnumerable<string>> GetFunctionsAsync(int operatorId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT f.function_name
            FROM functions f
            INNER JOIN operator_function_assoc ofa ON ofa.functions_id_function = f.id_function
            WHERE ofa.operators_id_operator = @operatorId
              AND f.active = 1";

        using var conn = await db.CreateOpenConnectionAsync(ct);
        return await conn.QueryAsync<string>(
            new CommandDefinition(sql, new { operatorId }, cancellationToken: ct));
    }
}

public class ConfigurationRepository(IDatabaseConnectionFactory db) : IConfigurationRepository
{
    public async Task<string?> GetValueAsync(string name, CancellationToken ct = default)
    {
        using var conn = await db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition(
                "SELECT value FROM configuration WHERE name = @name",
                new { name }, cancellationToken: ct));
    }

    public async Task SetValueAsync(string name, string value, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO configuration (name, value)
            VALUES (@name, @value)
            ON DUPLICATE KEY UPDATE value = @value";

        using var conn = await db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(sql, new { name, value }, cancellationToken: ct));
    }

    public async Task<Dictionary<string, string?>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await db.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<ConfigurationEntry>(
            new CommandDefinition(
                "SELECT name, value FROM configuration",
                cancellationToken: ct));
        return rows.ToDictionary(r => r.Name, r => r.Value);
    }
}
