namespace CardPass3.WPF.Data.Models;

/// <summary>Raw event row as stored in the database.</summary>
public class DbEvent
{
    public int IdEvent { get; set; }
    public DateTime? DatetimeUtc { get; set; }
    public DateTime? DatetimeLocal { get; set; }
    public int? UsersIdUser { get; set; }
    public string? Incidence { get; set; }
    public int ReadersIdReader { get; set; }
    public DateTime Modified { get; set; }
}

/// <summary>Enriched event for grid display — joins reader description and user name.</summary>
public class EventRow
{
    public int IdEvent { get; set; }
    public DateTime? DatetimeLocal { get; set; }
    public string? UserName { get; set; }
    public string? Surname { get; set; }
    public string? Incidence { get; set; }
    public string ReaderDescription { get; set; } = string.Empty;
}
