using CardPass3.WPF.Data.Models;
using CardPass3.WPF.Data.Repositories.Interfaces;
using CardPass3.WPF.Services.Database;
using Dapper;

namespace CardPass3.WPF.Data.Repositories;

public class AreaRepository(IDatabaseConnectionFactory db) : IAreaRepository
{
    private const string BaseSelect = @"
        SELECT id_area          AS IdArea,
               area_name        AS AreaName,
               area_description AS AreaDescription,
               parent_id_area   AS ParentIdArea,
               deleted,
               show_area        AS ShowArea,
               order_areas      AS OrderAreas
        FROM areas";

    public async Task<IEnumerable<Area>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await db.CreateOpenConnectionAsync(ct);
        return await conn.QueryAsync<Area>(
            new CommandDefinition($"{BaseSelect} WHERE deleted = 0 AND id_area > 1 ORDER BY area_name",
                cancellationToken: ct));
    }

    public async Task<Area?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var conn = await db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Area>(
            new CommandDefinition($"{BaseSelect} WHERE id_area = @id", new { id }, cancellationToken: ct));
    }
}
