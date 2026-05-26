using GridAcademy.Data.Entities.Exam;

namespace GridAcademy.Modules.AiGeneration.Domain.Entities;

/// <summary>Per-exam AI generation configuration. One row per ExamPage.</summary>
public class AiExamConfig
{
    public int  Id           { get; set; }
    public Guid ExamPageId   { get; set; }
    public bool IsAiEnabled  { get; set; } = false;
    public string? Notes     { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigations ───────────────────────────────────────────────────────
    public ExamPage ExamPage { get; set; } = null!;
    public ICollection<AiExamSection>          Sections          { get; set; } = [];
    public ICollection<GenerationPromptTemplate> PromptTemplates { get; set; } = [];
}
