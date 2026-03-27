namespace GridAcademy.Data.Entities.Assessment;

public class TestAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AssignmentId { get; set; }

    /// <summary>Denormalized FK → users.id for query convenience.</summary>
    public Guid StudentId { get; set; }

    /// <summary>Denormalized FK → tests.id for query convenience.</summary>
    public Guid TestId { get; set; }

    /// <summary>1-based counter within the assignment (1st attempt, 2nd attempt, etc.).</summary>
    public int AttemptNumber { get; set; }

    public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SubmittedAt { get; set; }

    public int DurationSecondsUsed { get; set; } = 0;

    public decimal? TotalMarksObtained { get; set; }

    public decimal? TotalMarksPossible { get; set; }

    public decimal? Percentage { get; set; }

    public bool? IsPassed { get; set; }

    /// <summary>JSON array of proctoring / tab-switch violation events. Max 4000 chars.</summary>
    public string? ViolationLog { get; set; }

    // ── Navigations ────────────────────────────────────────────────────────
    public TestAssignment Assignment { get; set; } = null!;

    public ICollection<AttemptQuestion> Questions { get; set; } = [];

    public ICollection<AttemptAnswer> Answers { get; set; } = [];

    public ICollection<AttemptSectionResult> SectionResults { get; set; } = [];
}
