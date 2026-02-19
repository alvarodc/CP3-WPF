namespace CardPass3.WPF.Data.Models;

public class UserGroup
{
    public int IdUserGroup { get; set; }
    public string UsergroupName { get; set; } = string.Empty;
    public string? UsergroupDescription { get; set; }
    public bool CardAuth { get; set; } = true;
    public bool PinAuth { get; set; }
    public bool BarcodeAuth { get; set; }
    public string FingerprintAuth { get; set; } = "No";
    public bool Default { get; set; }
    public DateTime Modified { get; set; }
}
