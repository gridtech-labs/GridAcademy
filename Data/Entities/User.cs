namespace GridAcademy.Data.Entities;

/// <summary>
/// Represents a platform user. Role is stored as a simple string ("Admin" / "User").
/// Keeping it flat avoids an extra roles table for a small-scale system.
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>"Admin" | "Instructor" | "User" | "Student" | "Provider"</summary>
    public string Role { get; set; } = "User";

    /// <summary>Mobile number for OTP-based login (optional).</summary>
    public string? Phone { get; set; }

    /// <summary>
    /// FK to the tenant/client this user belongs to.
    /// Null only for SuperAdmin users who have platform-wide access.
    /// </summary>
    public int? ClientId { get; set; }

    /// <summary>Navigation to the owning client.</summary>
    public Client? Client { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    // Computed helper — not mapped to DB
    public string FullName => $"{FirstName} {LastName}".Trim();
}
