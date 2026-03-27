using System.ComponentModel.DataAnnotations;
using GridAcademy.Data.Entities.Content;

namespace GridAcademy.Data.Entities.Exam;

/// <summary>
/// Rich exam information page (like sarkaripariksha.com).
/// Displays exam overview, eligibility, syllabus, pattern, dates.
/// Tests can be mapped via ExamPageTest.
/// </summary>
public class ExamPage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(300)]
    public string Title { get; set; } = "";

    /// <summary>URL-friendly slug, unique among published exams. e.g. up-home-guards</summary>
    [Required, MaxLength(300)]
    public string Slug { get; set; } = "";

    /// <summary>Short one-liner shown in cards/search results (HTML from editor).</summary>
    public string? ShortDescription { get; set; }

    // ── Rich content fields ────────────────────────────────────────────────
    public string? Overview         { get; set; }   // HTML / markdown
    public string? Eligibility      { get; set; }   // HTML
    public string? Syllabus         { get; set; }   // HTML
    public string? ExamPattern      { get; set; }   // HTML (table of sections, marks, duration)
    public string? ImportantDates   { get; set; }   // JSON: [{label:"Notification", date:"2026-04-01"}]
    public string? AdmitCard        { get; set; }   // HTML
    public string? ResultInfo       { get; set; }   // HTML
    public string? CutOff           { get; set; }   // HTML
    public string? HowToApply       { get; set; }   // HTML

    // ── Exam metadata ──────────────────────────────────────────────────────
    [MaxLength(300)]
    public string? ConductingBody   { get; set; }

    [MaxLength(500)]
    public string? OfficialWebsite  { get; set; }

    [MaxLength(500)]
    public string? NotificationUrl  { get; set; }

    // ── Categorization ────────────────────────────────────────────────────
    public int? ExamLevelId         { get; set; }   // FK → ExamLevel
    public int? ExamTypeId          { get; set; }   // FK → Content.ExamType (Engineering, Civil, etc.)

    // ── Media ─────────────────────────────────────────────────────────────
    public string? ThumbnailUrl     { get; set; }   // stored path e.g. /uploads/exams/xxx.jpg
    public string? BannerUrl        { get; set; }

    // ── Display ───────────────────────────────────────────────────────────
    public bool IsFeatured          { get; set; } = false;
    public bool IsActive            { get; set; } = true;
    public ExamPageStatus Status    { get; set; } = ExamPageStatus.Draft;
    public int ViewCount            { get; set; } = 0;
    public int SortOrder            { get; set; } = 0;

    // ── SEO ───────────────────────────────────────────────────────────────
    [MaxLength(300)]
    public string? MetaTitle        { get; set; }

    [MaxLength(500)]
    public string? MetaDescription  { get; set; }

    // ── Audit ─────────────────────────────────────────────────────────────
    public DateTime CreatedAt       { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt       { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy          { get; set; }
    public Guid? UpdatedBy          { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────
    public ExamLevel?              ExamLevel    { get; set; }
    public ExamType?               ExamType     { get; set; }
    public ICollection<ExamPageTest> Tests      { get; set; } = [];
}
