namespace CardPass3.WPF.Data.Models;

public class UserAttributeGroup
{
    public int IdUserAttributeGroup { get; set; }
    public string? GroupName { get; set; }
    public int GroupOrder { get; set; }
}

public class UserAttribute
{
    public int IdUserAttribute { get; set; }
    public string? AttributeName { get; set; }
    public string? AttributeType { get; set; }
    public bool Active { get; set; }
    public bool Show { get; set; }
    public int? UserAttributeGroupsIdGroup { get; set; }
    public int AttributeOrder { get; set; }
    public string? PrintArg { get; set; }
    public bool IsApiAttribute { get; set; }
    public string? ApiAttributeValue { get; set; }
}

public class UserAttributeValues
{
    public int IdUserAttributeValues { get; set; }
    public int? UsersIdUser { get; set; }
    public string? UserColumn1 { get; set; }
    public string? UserColumn2 { get; set; }
    public DateTime Modified { get; set; }
}
