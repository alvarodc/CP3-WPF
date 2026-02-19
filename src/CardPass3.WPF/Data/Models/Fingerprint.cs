namespace CardPass3.WPF.Data.Models;

public class Fingerprint
{
    public int IdFingerprint { get; set; }
    public int UsersIdUser { get; set; }
    public string? Finger { get; set; }
    public string? Flag { get; set; }
    public int TemplateFormat { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public DateTime Modified { get; set; }
}
