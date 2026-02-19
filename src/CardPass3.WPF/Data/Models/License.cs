namespace CardPass3.WPF.Data.Models;

public class License
{
    public int IdLicense { get; set; }
    public string LicenseDescription { get; set; } = string.Empty;
    public string FirstRun { get; set; } = string.Empty;
    public string LastRun { get; set; } = string.Empty;
    public string LicenseCode { get; set; } = string.Empty;
    public string Hid { get; set; } = string.Empty;
    public bool Active { get; set; }
}
