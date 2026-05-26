using GridAcademy.Data.Entities.Content;

namespace GridAcademy.Modules.AiGeneration.Domain.Entities;

/// <summary>
/// A topic within a section, enriched with syllabus text and reference questions
/// for the AI generation prompt.
/// </summary>
public class AiExamTopic
{
    public int  Id                 { get; set; }
    public int  AiExamSectionId    { get; set; }

    /// <summary>Maps to the existing topics table so approved questions have the right topic FK.</summary>
    public int? TopicId            { get; set; }

    /// <summary>Display name (may differ from Topic.Name for exam-specific phrasing).</summary>
    public string TopicName        { get; set; } = "";

    /// <summary>Admin pastes the syllabus excerpt here — injected into the generation prompt.</summary>
    public string? SyllabusText    { get; set; }

    /// <summary>
    /// JSON array of reference questions pasted by admin:
    /// [{question, options:[], correct_index, explanation, source_url}]
    /// Admin pastes plain MCQs; the UI auto-parses them into this structure.
    /// </summary>
    public string? ReferenceQuestionsJson { get; set; }

    public int  SortOrder          { get; set; } = 0;
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;

    // ── Navigations ───────────────────────────────────────────────────────
    public AiExamSection AiExamSection { get; set; } = null!;
    public Topic?        Topic         { get; set; }
}
