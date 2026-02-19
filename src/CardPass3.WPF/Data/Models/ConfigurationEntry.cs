namespace CardPass3.WPF.Data.Models;

public class ConfigurationEntry
{
    public int IdConfiguration { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
}
