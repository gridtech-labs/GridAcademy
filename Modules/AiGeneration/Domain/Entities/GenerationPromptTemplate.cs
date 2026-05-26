namespace GridAcademy.Modules.AiGeneration.Domain.Entities;

/// <summary>
/// Versioned prompt template. Never overwritten — each edit creates a new version row.
/// AI jobs record which version they used for full audit trail.
/// </summary>
public class GenerationPromptTemplate
{
    public int  Id              { get; set; }
    public int  AiExamConfigId  { get; set; }

    /// <summary>Null = applies to all sections of the exam; set = section-specific override.</summary>
    public int? AiExamSectionId { get; set; }

    public int     Version    { get; set; }
    public bool    IsActive   { get; set; } = true;
    public string  PromptText { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid?    CreatedBy { get; set; }

    // ── Navigations ───────────────────────────────────────────────────────
    public AiExamConfig   AiExamConfig   { get; set; } = null!;
    public AiExamSection? AiExamSection  { get; set; }
}
