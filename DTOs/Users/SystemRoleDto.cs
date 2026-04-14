using System.ComponentModel.DataAnnotations;

namespace GridAcademy.DTOs.Users;

public class SystemRoleDto
{
    public int     Id          { get; set; }
    public string  Name        { get; set; } = "";
    public string  DisplayName { get; set; } = "";
    public string? Description { get; set; }
    public string? Color       { get; set; }
    public bool    IsSystem    { get; set; }
    public bool    IsActive    { get; set; }
    public int     SortOrder   { get; set; }
    public int     UserCount   { get; set; }
    public DateTime CreatedAt  { get; set; }
}

public class CreateSystemRoleRequest
{
    [Required, MaxLength(50)]
    public string Name { get; set; } = "";

    [Required, MaxLength(100)]
    public string DisplayName { get; set; } = "";

    public string? Description { get; set; }

    /// <summary>Bootstrap color name: primary, secondary, success, danger, warning, info.</summary>
    public string Color { get; set; } = "secondary";

    public bool IsActive  { get; set; } = true;
    public int  SortOrder { get; set; } = 0;
}
