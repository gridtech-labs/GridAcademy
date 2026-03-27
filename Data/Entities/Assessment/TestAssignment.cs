using GridAcademy.Data.Entities;

namespace GridAcademy.Data.Entities.Assessment;

public class TestAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TestId { get; set; }

    public Guid StudentId { get; set; }

    public DateTime AvailableFrom { get; set; }

    public DateTime AvailableTo { get; set; }

    public int MaxAttempts { get; set; } = 1;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Nullable FK → users.id; tracks which admin/instructor created the assignment.</summary>
    public Guid? AssignedBy { get; set; }

    // ── Navigations ────────────────────────────────────────────────────────
    public Test Test { get; set; } = null!;

    public User Student { get; set; } = null!;

    public ICollection<TestAttempt> Attempts { get; set; } = [];
}
