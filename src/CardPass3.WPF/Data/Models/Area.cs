namespace CardPass3.WPF.Data.Models;

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
