namespace CardPass3.WPF.Data.Models;

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

    /// <summary>Effective IP for TCP connection — falls back to IpAddress if no override.</summary>
    public string EffectiveIp => !string.IsNullOrWhiteSpace(IpAddressEffective)
        ? IpAddressEffective
        : IpAddress;
}
