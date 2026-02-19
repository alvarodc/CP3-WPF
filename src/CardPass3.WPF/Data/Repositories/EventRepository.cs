using CardPass3.WPF.Data.Models;
using CardPass3.WPF.Data.Repositories.Interfaces;
using CardPass3.WPF.Services.Database;
using Dapper;

namespace CardPass3.WPF.Data.Repositories
{

public class EventRepository(IDatabaseConnectionFactory db) : IEventRepository
{
    /// <summary>
    /// Keyset pagination: instead of OFFSET (which degrades on large tables),
    /// we filter by id_event > lastIdEvent, which uses the PK index directly.
    /// First page: pass lastIdEvent = 0.
    /// </summary>
    public async Task<IEnumerable<EventRow>> GetPageAsync(
        EventFilter filter,
        int lastIdEvent,
        int pageSize,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT
                e.id_event          AS IdEvent,
                e.datetime_local    AS DatetimeLocal,
                u.user_name         AS UserName,
                u.surname           AS Surname,
                e.incidence         AS Incidence,
                r.reader_description AS ReaderDescription
            FROM events e
            INNER JOIN readers r ON r.id_reader = e.readers_id_reader
            LEFT  JOIN users   u ON u.id_user   = e.users_id_user
            WHERE e.id_event > @lastIdEvent
              /**filters**/
            ORDER BY e.id_event ASC
            LIMIT @pageSize";

        var parameters = new DynamicParameters();
        parameters.Add("lastIdEvent", lastIdEvent);
        parameters.Add("pageSize", pageSize);

        var conditions = new List<string>();

        if (filter.From.HasValue)
        {
            conditions.Add("AND e.datetime_local >= @from");
            parameters.Add("from", filter.From.Value);
        }
        if (filter.To.HasValue)
        {
            conditions.Add("AND e.datetime_local <= @to");
            parameters.Add("to", filter.To.Value);
        }
        if (filter.ReaderId.HasValue)
        {
            conditions.Add("AND e.readers_id_reader = @readerId");
            parameters.Add("readerId", filter.ReaderId.Value);
        }
        if (filter.UserId.HasValue)
        {
            conditions.Add("AND e.users_id_user = @userId");
            parameters.Add("userId", filter.UserId.Value);
        }
        if (!string.IsNullOrWhiteSpace(filter.Incidence))
        {
            conditions.Add("AND e.incidence = @incidence");
            parameters.Add("incidence", filter.Incidence);
        }

        sql = sql.Replace("/**filters**/", string.Join("\n              ", conditions));

        using var conn = await db.CreateOpenConnectionAsync(ct);
        return await conn.QueryAsync<EventRow>(
            new CommandDefinition(sql, parameters, cancellationToken: ct));
    }

    public async Task<int> GetCountAsync(EventFilter filter, CancellationToken ct = default)
    {
        var sql = @"
            SELECT COUNT(*)
            FROM events e
            WHERE 1=1
              /**filters**/";

        var parameters = new DynamicParameters();
        var conditions = new List<string>();

        if (filter.From.HasValue)
        {
            conditions.Add("AND e.datetime_local >= @from");
            parameters.Add("from", filter.From.Value);
        }
        if (filter.To.HasValue)
        {
            conditions.Add("AND e.datetime_local <= @to");
            parameters.Add("to", filter.To.Value);
        }
        if (filter.ReaderId.HasValue)
        {
            conditions.Add("AND e.readers_id_reader = @readerId");
            parameters.Add("readerId", filter.ReaderId.Value);
        }
        if (filter.UserId.HasValue)
        {
            conditions.Add("AND e.users_id_user = @userId");
            parameters.Add("userId", filter.UserId.Value);
        }

        sql = sql.Replace("/**filters**/", string.Join("\n              ", conditions));

        using var conn = await db.CreateOpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, parameters, cancellationToken: ct));
    }

    public async Task InsertAsync(DbEvent ev, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO events (datetime_utc, datetime_local, users_id_user, incidence, readers_id_reader)
            VALUES (@DatetimeUtc, @DatetimeLocal, @UsersIdUser, @Incidence, @ReadersIdReader)";

        using var conn = await db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(sql, ev, cancellationToken: ct));
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        using var conn = await db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            new CommandDefinition("DELETE FROM events WHERE id_event = @id", new { id }, cancellationToken: ct));
    }
}
