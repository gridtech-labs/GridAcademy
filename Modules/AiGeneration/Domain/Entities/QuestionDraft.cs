using GridAcademy.Data.Entities.Content;

namespace GridAcademy.Modules.AiGeneration.Domain.Entities;

/// <summary>
/// AI-generated question awaiting human review.
/// On approval it is converted to a Question row (or QuestionTranslation for non-English).
/// </summary>
public class QuestionDraft
{
    public int  Id                 { get; set; }
    public int  GenerationJobId    { get; set; }

    // ── Classification (carried from section/topic at generation time) ────
    public int? SubjectId          { get; set; }
    public int? TopicId            { get; set; }
    public int? DifficultyLevelId  { get; set; }

    public string Language         { get; set; } = "en";

    // ── AI output ────────────────────────────────────────────────────────
    public string  QuestionText    { get; set; } = "";

    /// <summary>JSON: [{label:"A",text:"...",is_correct:false}, ...]</summary>
    public string  OptionsJson     { get; set; } = "[]";
    public int     CorrectIndex    { get; set; } = 0;
    public string? Explanation     { get; set; }

    /// <summary>JSON: [{op:"multiply",operands:[12,4],result:48}, ...]</summary>
    public string? CalculationStepsJson { get; set; }
    public string? DifficultyEstimate   { get; set; }
    public int?    EstimatedSeconds     { get; set; }

    // ── Verification flags ────────────────────────────────────────────────
    /// <summary>JSON object: {self_verification_mismatch:bool, math_mismatch:bool, possible_duplicate:{matched_id,score}, sanity_fail:string[]}</summary>
    public string FlagsJson        { get; set; } = "{}";

    // ── Review ────────────────────────────────────────────────────────────
    public DraftStatus Status      { get; set; } = DraftStatus.PendingReview;
    public Guid?   ReviewerId      { get; set; }
    public string? ReviewNotes     { get; set; }
    public DateTime? ReviewedAt    { get; set; }

    /// <summary>Set after approval — the Question.Id that was created from this draft.</summary>
    public Guid? ApprovedQuestionId { get; set; }

    /// <summary>For translation drafts: the source English question this translates.</summary>
    public Guid? SourceQuestionId  { get; set; }

    // ── Provenance ────────────────────────────────────────────────────────
    public string? Model                 { get; set; }
    public int?    PromptTemplateVersion { get; set; }
    public DateTime CreatedAt            { get; set; } = DateTime.UtcNow;

    // ── Navigations ───────────────────────────────────────────────────────
    public GenerationJob    GenerationJob   { get; set; } = null!;
    public Subject?         Subject         { get; set; }
    public Topic?           Topic           { get; set; }
    public DifficultyLevel? DifficultyLevel { get; set; }
}
