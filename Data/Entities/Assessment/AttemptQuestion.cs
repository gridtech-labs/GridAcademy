using GridAcademy.Data.Entities.Content;

namespace GridAcademy.Data.Entities.Assessment;

/// <summary>
/// Snapshot of which questions were randomly selected for a given attempt,
/// plus per-question navigation state (visited, marked for review).
/// </summary>
public class AttemptQuestion
{
    public int Id { get; set; }

    /// <summary>FK → test_attempts.id; cascade delete.</summary>
    public Guid AttemptId { get; set; }

    /// <summary>FK → questions.id; restrict delete to preserve history.</summary>
    public Guid QuestionId { get; set; }

    /// <summary>0-based index of the TestSection this question was drawn from.</summary>
    public int SectionIndex { get; set; }

    /// <summary>Snapshot of the section name at the time the attempt was created.</summary>
    public string SectionName { get; set; } = string.Empty;

    /// <summary>Global 1-based question number shown to the student.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>1-based position of this question within its section.</summary>
    public int DisplayOrderInSection { get; set; }

    /// <summary>Snapshot of marks awarded for a correct answer (captured at attempt creation).</summary>
    public decimal MarksForCorrect { get; set; }

    /// <summary>Snapshot of marks deducted for a wrong answer (captured at attempt creation).</summary>
    public decimal NegativeMarks { get; set; }

    public bool IsVisited { get; set; } = false;

    public bool IsMarkedForReview { get; set; } = false;

    // ── Navigations ────────────────────────────────────────────────────────
    public TestAttempt Attempt { get; set; } = null!;

    public Question Question { get; set; } = null!;
}
