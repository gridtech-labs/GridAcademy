using GridAcademy.Data.Entities.Content;

namespace GridAcademy.Modules.AiGeneration.Domain.Entities;

/// <summary>
/// A section inside an AI exam config (e.g. "Reasoning", "Quantitative Aptitude").
/// Maps to an existing Subject so approved questions land in the right pool.
/// </summary>
public class AiExamSection
{
    public int  Id                { get; set; }
    public int  AiExamConfigId    { get; set; }
    public string SectionName     { get; set; } = "";

    /// <summary>Maps to the existing subjects table so approved questions are queryable by subject.</summary>
    public int? SubjectId         { get; set; }

    public decimal? WeightagePercent  { get; set; }
    public int  NumberOfQuestions     { get; set; } = 25;
    public int  SortOrder             { get; set; } = 0;

    /// <summary>Default marks_master.id for this section (used when promoting drafts to questions).</summary>
    public int? MarksId               { get; set; }
    public int? NegativeMarksId       { get; set; }

    public DateTime CreatedAt         { get; set; } = DateTime.UtcNow;

    // ── Navigations ───────────────────────────────────────────────────────
    public AiExamConfig   AiExamConfig { get; set; } = null!;
    public Subject?       Subject      { get; set; }
    public ICollection<AiExamTopic>              Topics           { get; set; } = [];
    public ICollection<GenerationPromptTemplate> PromptTemplates  { get; set; } = [];
}
