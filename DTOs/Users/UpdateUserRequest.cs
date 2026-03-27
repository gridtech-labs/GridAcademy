using System.ComponentModel.DataAnnotations;

namespace GridAcademy.DTOs.Users;

public class UpdateUserRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Allowed values: "Admin", "User".</summary>
    public string Role { get; set; } = "User";

    public bool IsActive { get; set; } = true;
}
