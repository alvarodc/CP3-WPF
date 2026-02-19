using CardPass3.WPF.Data.Models;

namespace CardPass3.WPF.Data.Repositories.Interfaces;

public interface IReaderRepository
{
    Task<IEnumerable<Reader>> GetAllEnabledAsync(CancellationToken ct = default);
    Task<IEnumerable<Reader>> GetAllAsync(CancellationToken ct = default);
    Task<Reader?> GetByIdAsync(int id, CancellationToken ct = default);
    Task UpdateAsync(Reader reader, CancellationToken ct = default);
    Task InsertAsync(Reader reader, CancellationToken ct = default);
    Task SoftDeleteAsync(int id, CancellationToken ct = default);
}

public interface IOperatorRepository
{
    Task<Operator?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IEnumerable<string>> GetFunctionsAsync(int operatorId, CancellationToken ct = default);
}

public interface IEventRepository
{
    /// <summary>
    /// Paged query using keyset pagination for performance on large tables.
    /// Pass lastIdEvent = 0 for the first page.
    /// </summary>
    Task<IEnumerable<EventRow>> GetPageAsync(
        EventFilter filter,
        int lastIdEvent,
        int pageSize,
        CancellationToken ct = default);

    Task<int> GetCountAsync(EventFilter filter, CancellationToken ct = default);
    Task InsertAsync(Event ev, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface IConfigurationRepository
{
    Task<string?> GetValueAsync(string name, CancellationToken ct = default);
    Task SetValueAsync(string name, string value, CancellationToken ct = default);
    Task<Dictionary<string, string?>> GetAllAsync(CancellationToken ct = default);
}

/// <summary>Filter parameters for event queries.</summary>
public record EventFilter(
    DateTime? From = null,
    DateTime? To = null,
    int? ReaderId = null,
    int? UserId = null,
    string? Incidence = null
);
