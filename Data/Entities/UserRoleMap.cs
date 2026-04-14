namespace GridAcademy.Data.Entities;

public class UserRoleMap
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public int RoleId { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public Guid? AssignedBy { get; set; }

    // ── Navigations ─────────────────────────────────────────────────────────
    public User User { get; set; } = null!;

    public SystemRole Role { get; set; } = null!;
}
