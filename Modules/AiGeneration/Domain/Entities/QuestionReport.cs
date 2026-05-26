namespace GridAcademy.Modules.AiGeneration.Domain.Entities;

/// <summary>Student-reported issue on a question. Feeds the quality dashboard.</summary>
public class QuestionReport
{
    public int    Id            { get; set; }
    public Guid   QuestionId   { get; set; }
    public Guid?  ReporterId   { get; set; }

    /// <summary>wrong_answer | unclear | offensive | outdated | other</summary>
    public string ReportType   { get; set; } = "other";
    public string ReportText   { get; set; } = "";

    public QuestionReportStatus Status { get; set; } = QuestionReportStatus.Open;
    public Guid?     ResolvedBy  { get; set; }
    public DateTime? ResolvedAt  { get; set; }
    public string?   Resolution  { get; set; }

    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
}
