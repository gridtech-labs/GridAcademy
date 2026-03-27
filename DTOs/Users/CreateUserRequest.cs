using System.ComponentModel.DataAnnotations;

namespace GridAcademy.DTOs.Users;

public class CreateUserRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Min 8 chars, must include uppercase, digit, and special character.</summary>
    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Allowed values: "Admin", "User". Defaults to "User".</summary>
    public string Role { get; set; } = "User";
}
