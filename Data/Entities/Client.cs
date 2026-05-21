namespace GridAcademy.Data.Entities;

/// <summary>
/// Represents a tenant/client organisation in the multi-tenant platform.
/// Each user (Admin/Instructor/Student) is mapped to exactly one client.
/// The built-in "GridAcademy" client (seeded on startup) is the default.
/// SuperAdmin users have cross-client visibility and are NOT constrained by ClientId.
/// </summary>
public class Client
{
    public int Id { get; set; }

    /// <summary>Display name, e.g. "Apex Institute".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-safe slug, e.g. "apex-institute".</summary>
    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? LogoUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // ── Navigation ──────────────────────────────────────────────────────────
    public ICollection<User> Users { get; set; } = [];
}
