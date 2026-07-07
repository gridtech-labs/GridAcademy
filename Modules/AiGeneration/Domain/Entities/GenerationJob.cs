using GridAcademy.Data.Entities.Exam;

namespace GridAcademy.Modules.AiGeneration.Domain.Entities;

/// <summary>One AI question generation run. Enqueued by admin, executed by Hangfire worker.</summary>
public class GenerationJob
{
    public int  Id               { get; set; }
    public Guid ExamPageId       { get; set; }
    public int? AiExamSectionId  { get; set; }
    public int? AiExamTopicId    { get; set; }

    /// <summary>
    /// Optional target Test. When set, approved questions from this job are
    /// added directly to this Test (mapped via test_questions).
    /// </summary>
    public Guid? TestId          { get; set; }

    public string Difficulty     { get; set; } = "medium"; // easy | medium | hard
    public string Language       { get; set; } = "en";     // en | hi
    public int    Count          { get; set; } = 10;
    public string? Notes         { get; set; }

    public GenerationJobStatus Status         { get; set; } = GenerationJobStatus.Queued;
    public int   Generated        { get; set; } = 0;
    public int   AutoFlagged      { get; set; } = 0;
    public int   AutoRejected     { get; set; } = 0;
    public string? ErrorMessage   { get; set; }
    public string? HangfireJobId  { get; set; }
    public int?  PromptTemplateVersion { get; set; }

    public DateTime  CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public Guid?     CreatedBy   { get; set; }

    // ── Navigations ───────────────────────────────────────────────────────
    public ExamPage       ExamPage      { get; set; } = null!;
    public AiExamSection? AiExamSection { get; set; }
    public AiExamTopic?   AiExamTopic   { get; set; }
    public ICollection<QuestionDraft> Drafts { get; set; } = [];
}
