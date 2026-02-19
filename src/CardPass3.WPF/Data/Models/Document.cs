namespace CardPass3.WPF.Data.Models;

public class Document
{
    public int IdDocument { get; set; }
    public string? DocumentDescription { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public bool Deleted { get; set; }
    public DateTime Modified { get; set; }
}
