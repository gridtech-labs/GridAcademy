namespace GridAcademy.Data.Entities;

public class SystemRole
{
    public int Id { get; set; }

    /// <summary>Internal key used in auth (e.g. "Admin", "Instructor").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Human-friendly label shown in the UI (e.g. "Administrator").</summary>
    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Bootstrap badge color class, e.g. "danger", "primary".</summary>
    public string? Color { get; set; }

    /// <summary>System roles are seeded and cannot be deleted.</summary>
    public bool IsSystem { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigations ─────────────────────────────────────────────────────────
    public ICollection<UserRoleMap> UserRoleMaps { get; set; } = [];
}
