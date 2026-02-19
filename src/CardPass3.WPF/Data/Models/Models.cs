namespace CardPass3.WPF.Data.Models;

// ─── readers ────────────────────────────────────────────────────────────────
public class Reader
{
    public int IdReader { get; set; }
    public string ReaderDescription { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? IpAddressEffective { get; set; }
    public int Port { get; set; } = 5000;
    public int PortWs { get; set; } = 5002;
    public string? UniqueName { get; set; }
    public int ControlType { get; set; }
    public bool Enabled { get; set; }
    public bool Deleted { get; set; }
    public int AreasIdArea { get; set; }
    public int Driver { get; set; }
    public int VerifyMode { get; set; }
    public int LicensesIdLicense { get; set; }
    public bool UseReaderDelay { get; set; }
    public int ReaderDelay { get; set; }
    public DateTime Modified { get; set; }

    /// <summary>Effective IP to use for TCP connection (falls back to IpAddress).</summary>
    public string EffectiveIp => !string.IsNullOrWhiteSpace(IpAddressEffective)
        ? IpAddressEffective
        : IpAddress;
}

// ─── operators ──────────────────────────────────────────────────────────────
public class Operator
{
    public int IdOperator { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string OperatorDescription { get; set; } = string.Empty;

    public List<string> FunctionNames { get; set; } = new();
}

// ─── events ─────────────────────────────────────────────────────────────────
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

// ─── users ──────────────────────────────────────────────────────────────────
public class User
{
    public int IdUser { get; set; }
    public string? RegNumber { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? CardNumber { get; set; }
    public string Surname { get; set; } = string.Empty;
    public int? TemplatesIdTemplate { get; set; }
    public bool Active { get; set; }
    public bool AntiPassback { get; set; }
    public DateTime? DateExpiration { get; set; }
    public DateTime? DateBegin { get; set; }
    public int CurrentIdArea { get; set; }
    public bool Deleted { get; set; }
    public string? Status { get; set; }
    public int UserGroupsIdUserGroup { get; set; }
}

// ─── areas ──────────────────────────────────────────────────────────────────
public class Area
{
    public int IdArea { get; set; }
    public string? AreaName { get; set; }
    public string? AreaDescription { get; set; }
    public int ParentIdArea { get; set; }
    public bool Deleted { get; set; }
    public bool ShowArea { get; set; }
    public int OrderAreas { get; set; }
}

// ─── configuration ──────────────────────────────────────────────────────────
public class ConfigurationEntry
{
    public int IdConfiguration { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
}
