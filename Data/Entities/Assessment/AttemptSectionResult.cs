namespace GridAcademy.Data.Entities.Assessment;

/// <summary>
/// Per-section scoring breakdown, populated only at submission time.
/// Provides a fast summary without re-evaluating individual answers.
/// </summary>
public class AttemptSectionResult
{
    public int Id { get; set; }

    /// <summary>FK → test_attempts.id; cascade delete.</summary>
    public Guid AttemptId { get; set; }

    /// <summary>0-based index matching TestSection order within the test.</summary>
    public int SectionIndex { get; set; }

    /// <summary>Snapshot of the section name at submission time.</summary>
    public string SectionName { get; set; } = string.Empty;

    public int TotalQuestions { get; set; }

    public int Attempted { get; set; }

    public int Correct { get; set; }

    public int Incorrect { get; set; }

    public int Unattempted { get; set; }

    public decimal MarksObtained { get; set; }

    public decimal MaxMarks { get; set; }

    // ── Navigations ────────────────────────────────────────────────────────
    public TestAttempt Attempt { get; set; } = null!;
}
