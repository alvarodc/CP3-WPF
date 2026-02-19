namespace CardPass3.WPF.Data.Models;

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
