namespace GridAcademy.Data.Entities;

/// <summary>
/// A named batch of users within a client tenant.
/// Groups can later be bulk-assigned to Tests, Programs, and Assessments.
/// </summary>
public class Group
{
    public int Id { get; set; }

    /// <summary>Human-readable label, e.g. "Batch 2026 – Morning".</summary>
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Owning client. Null only for SuperAdmin-level groups (platform-wide).</summary>
    public int? ClientId { get; set; }
    public Client? Client { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>User who created this group.</summary>
    public Guid? CreatedBy { get; set; }

    // ── Navigation ──────────────────────────────────────────────────────────
    public ICollection<UserGroup> UserGroups { get; set; } = [];
}
