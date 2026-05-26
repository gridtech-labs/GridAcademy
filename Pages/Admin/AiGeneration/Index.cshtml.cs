using GridAcademy.Data;
using GridAcademy.Modules.AiGeneration.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Admin.AiGeneration;

[Authorize(Roles = "SuperAdmin,Admin,Instructor")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    // ── Dashboard stats ─────────────────────────────────────────────────────
    public int TotalDrafts        { get; private set; }
    public int PendingReview      { get; private set; }
    public int ApprovedToday      { get; private set; }
    public int RejectedToday      { get; private set; }
    public int JobsToday          { get; private set; }
    public int AiQuestionsTotal   { get; private set; }
    public int ExamsWithAiEnabled { get; private set; }

    // ── Recent jobs ─────────────────────────────────────────────────────────
    public List<RecentJobRow> RecentJobs { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var today = DateTime.UtcNow.Date;

        TotalDrafts        = await _db.QuestionDrafts.CountAsync();
        PendingReview      = await _db.QuestionDrafts.CountAsync(d => d.Status == DraftStatus.PendingReview);
        ApprovedToday      = await _db.QuestionDrafts.CountAsync(d =>
                                d.Status == DraftStatus.Approved && d.ReviewedAt >= today);
        RejectedToday      = await _db.QuestionDrafts.CountAsync(d =>
                                d.Status == DraftStatus.Rejected && d.ReviewedAt >= today);
        JobsToday          = await _db.GenerationJobs.CountAsync(j => j.CreatedAt >= today);
        AiQuestionsTotal   = await _db.Questions.CountAsync(q => q.IsAiGenerated);
        ExamsWithAiEnabled = await _db.AiExamConfigs.CountAsync(c => c.IsAiEnabled);

        RecentJobs = await _db.GenerationJobs
            .OrderByDescending(j => j.CreatedAt)
            .Take(10)
            .Select(j => new RecentJobRow(
                j.Id,
                j.Difficulty,
                j.Language,
                j.Count,
                j.Generated,
                j.AutoFlagged,
                j.Status,
                j.CreatedAt,
                j.CompletedAt))
            .ToListAsync();
    }

    public record RecentJobRow(
        int Id, string Difficulty, string Language,
        int Count, int Generated, int AutoFlagged,
        GenerationJobStatus Status, DateTime CreatedAt, DateTime? CompletedAt);
}
