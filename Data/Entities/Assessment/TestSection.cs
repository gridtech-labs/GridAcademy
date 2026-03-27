using GridAcademy.Data.Entities.Content;

namespace GridAcademy.Data.Entities.Assessment;

public class TestSection
{
    public int Id { get; set; }

    public Guid TestId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SubjectId { get; set; }

    /// <summary>Nullable FK → difficulty_levels.id; null means any difficulty is eligible.</summary>
    public int? DifficultyLevelId { get; set; }

    public int QuestionCount { get; set; }

    public decimal MarksPerQuestion { get; set; }

    /// <summary>Set to 0 when negative marking is not applied to this section.</summary>
    public decimal NegativeMarksPerQuestion { get; set; }

    public int SortOrder { get; set; } = 0;

    // ── Navigations ────────────────────────────────────────────────────────
    public Test Test { get; set; } = null!;

    public Subject Subject { get; set; } = null!;

    public DifficultyLevel? DifficultyLevel { get; set; }
}
