using CardPass3.WPF.Data.Models;
using CardPass3.WPF.Data.Repositories.Interfaces;
using CardPass3.WPF.Services.Database;
using Dapper;

namespace CardPass3.WPF.Data.Repositories
{

public class ReaderRepository(IDatabaseConnectionFactory db) : IReaderRepository
{
    private const string BaseSelect = @"
        SELECT id_reader        AS IdReader,
               reader_description AS ReaderDescription,
               ip_address       AS IpAddress,
               ip_address_effective AS IpAddressEffective,
               port,
               port_ws          AS PortWs,
               unique_name      AS UniqueName,
               control_type     AS ControlType,
               enabled,
               deleted,
               areas_id_area    AS AreasIdArea,
               driver,
               verify_mode      AS VerifyMode,
               licenses_id_license AS LicensesIdLicense,
               use_reader_delay AS UseReaderDelay,
               reader_delay     AS ReaderDelay,
               modified
        FROM readers";

    public async Task<IEnumerable<Reader>> GetAllEnabledAsync(CancellationToken ct = default)
    {
        using var conn = await db.CreateOpenConnectionAsync(ct);
        return await conn.QueryAsync<Reader>(
            new CommandDefinition($"{BaseSelect} WHERE deleted = 0 AND enabled = 1", cancellationToken: ct));
    }

    public async Task<IEnumerable<Reader>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await db.CreateOpenConnectionAsync(ct);
        return await conn.QueryAsync<Reader>(
            new CommandDefinition($"{BaseSelect} WHERE deleted = 0", cancellationToken: ct));
    }

    public async Task<Reader?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var conn = await db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Reader>(
            new CommandDefinition($"{BaseSelect} WHERE id_reader = @id", new { id }, cancellationToken: ct));
    }

    public async Task UpdateAsync(Reader reader, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE readers SET
                reader_description   = @ReaderDescription,
                ip_address           = @IpAddress,
                ip_address_effective = @IpAddressEffective,
                port                 = @Port,
                port_ws              = @PortWs,
                unique_name          = @UniqueName,
                control_type         = @ControlType,
                enabled              = @Enabled,
                areas_id_area        = @AreasIdArea,
                driver               = @Driver,
                verify_mode          = @VerifyMode,
                use_reader_delay     = @UseReaderDelay,
                reader_delay         = @ReaderDelay
            WHERE id_reader = @IdReader";

        using var conn = await db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(sql, reader, cancellationToken: ct));
    }

    public async Task InsertAsync(Reader reader, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO readers
                (reader_description, ip_address, ip_address_effective, port, port_ws,
                 unique_name, control_type, enabled, areas_id_area, driver,
                 verify_mode, licenses_id_license, use_reader_delay, reader_delay)
            VALUES
                (@ReaderDescription, @IpAddress, @IpAddressEffective, @Port, @PortWs,
                 @UniqueName, @ControlType, @Enabled, @AreasIdArea, @Driver,
                 @VerifyMode, @LicensesIdLicense, @UseReaderDelay, @ReaderDelay)";

        using var conn = await db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(sql, reader, cancellationToken: ct));
    }

    public async Task SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        using var conn = await db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            new CommandDefinition(
                "UPDATE readers SET deleted = 1, enabled = 0 WHERE id_reader = @id",
                new { id }, cancellationToken: ct));
    }
}
}
