namespace GridAcademy.Data.Entities.Assessment;

/// <summary>
/// Stores the student's answer per question within an attempt.
/// This record is upserted by the API each time the student saves or changes an answer.
/// </summary>
public class AttemptAnswer
{
    public int Id { get; set; }

    /// <summary>FK → test_attempts.id; cascade delete.</summary>
    public Guid AttemptId { get; set; }

    /// <summary>FK → questions.id; restrict delete to preserve answer history.</summary>
    public Guid QuestionId { get; set; }

    /// <summary>
    /// Comma-separated int option IDs for MCQ/MSQ answers (e.g. "3" or "3,7").
    /// Null for NAT questions.
    /// </summary>
    public string? SelectedOptionIds { get; set; }

    /// <summary>
    /// Numeric answer entered by the student for NAT questions.
    /// Null for MCQ/MSQ questions.
    /// </summary>
    public decimal? NumericalValue { get; set; }

    /// <summary>True when the student explicitly cleared a previously saved answer.</summary>
    public bool IsClear { get; set; } = false;

    /// <summary>Updated on every save; tracks the last time this answer was written.</summary>
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    // ── Navigations ────────────────────────────────────────────────────────
    public TestAttempt Attempt { get; set; } = null!;
}
