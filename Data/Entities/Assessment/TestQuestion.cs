namespace GridAcademy.Data.Entities.Assessment;

/// <summary>
/// Direct assignment of a specific Question to a Test.
/// Populated when questions are imported with "Map to Test" destination.
/// </summary>
public class TestQuestion
{
    public int Id { get; set; }

    /// <summary>FK → tests.id</summary>
    public Guid TestId { get; set; }

    /// <summary>FK → questions.id</summary>
    public Guid QuestionId { get; set; }

    /// <summary>Display order within the test (1-based).</summary>
    public int SortOrder { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // ── Navigations ────────────────────────────────────────────────────────
    public Test Test { get; set; } = null!;

    public GridAcademy.Data.Entities.Content.Question Question { get; set; } = null!;
}
