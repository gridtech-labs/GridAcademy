using GridAcademy.Data.Entities.Content;

namespace GridAcademy.Data.Entities.Assessment;

public class Test
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    /// <summary>Optional HTML instructions shown to the student before starting.</summary>
    public string? Instructions { get; set; }

    public int DurationMinutes { get; set; }

    public decimal PassingPercent { get; set; }

    public bool NegativeMarkingEnabled { get; set; } = false;

    public int ExamTypeId { get; set; }

    public TestStatus Status { get; set; } = TestStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    // ── Navigations ────────────────────────────────────────────────────────
    public ExamType ExamType { get; set; } = null!;

    public ICollection<TestSection> Sections { get; set; } = [];

    public ICollection<TestAssignment> Assignments { get; set; } = [];
}
