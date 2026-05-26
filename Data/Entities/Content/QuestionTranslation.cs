namespace GridAcademy.Data.Entities.Content;

/// <summary>
/// A language variant of an existing canonical question.
/// The canonical (usually English) question lives in the questions table.
/// Each additional language (Hindi, Tamil, etc.) gets one row here.
/// </summary>
public class QuestionTranslation
{
    public int  Id               { get; set; }

    /// <summary>FK → questions.id — the canonical question this is a translation of.</summary>
    public Guid SourceQuestionId { get; set; }

    /// <summary>BCP-47 language code: "hi", "ta", "te", "mr", etc. Never "en" — that stays in questions.</summary>
    public string Language { get; set; } = "hi";

    public string  Text     { get; set; } = "";
    public string? Solution { get; set; }

    /// <summary>JSON array: [{label:"A",text:"...",is_correct:false}, ...]</summary>
    public string OptionsJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid?    CreatedBy { get; set; }

    // ── Navigations ───────────────────────────────────────────────────────
    public Question SourceQuestion { get; set; } = null!;
}
