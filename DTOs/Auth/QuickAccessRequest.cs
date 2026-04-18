using System.ComponentModel.DataAnnotations;

namespace GridAcademy.DTOs.Auth;

/// <summary>
/// Frictionless entry for students — no password required.
/// Backend auto-registers if the email is new, or silently logs in if it exists.
/// </summary>
public class QuickAccessRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>10-digit Indian mobile number (6–9 prefix).</summary>
    [Required, RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit mobile number.")]
    public string Mobile { get; set; } = string.Empty;
}
