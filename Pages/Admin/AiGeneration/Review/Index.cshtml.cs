using GridAcademy.Data;
using GridAcademy.Modules.AiGeneration.Domain.Entities;
using GridAcademy.Modules.AiGeneration.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GridAcademy.Pages.Admin.AiGeneration.Review;

[Authorize(Roles = "SuperAdmin,Admin,Instructor")]
public class IndexModel : PageModel
{
    private readonly AppDbContext  _db;
    private readonly ReviewService _review;
    public IndexModel(AppDbContext db, ReviewService review)
    {
        _db     = db;
        _review = review;
    }

    // ── Filters ────────────────────────────────────────────────────────────
    [BindProperty(SupportsGet = true)] public int?   JobId      { get; set; }
    [BindProperty(SupportsGet = true)] public int    StatusTab  { get; set; } = 1; // 1=Pending,2=Approved,3=Rejected
    [BindProperty(SupportsGet = true)] public int    PageNum    { get; set; } = 1;
    private const int PageSize = 10;

    public int TotalCount  { get; private set; }
    public List<DraftRow> Drafts { get; private set; } = [];
    public bool TablesReady { get; private set; } = true;

    // ── Reference data ─────────────────────────────────────────────────────
    public List<(int Id, string Name)> SubjectOptions   { get; private set; } = [];
    public List<(int Id, string Name)> TopicOptions     { get; private set; } = [];
    public List<(int Id, string Name)> DifficultyOptions { get; private set; } = [];
    public int PendingCount  { get; private set; }
    public int ApprovedCount { get; private set; }
    public int RejectedCount { get; private set; }

    // ── Edit form ──────────────────────────────────────────────────────────
    [BindProperty] public int     EditDraftId      { get; set; }
    [BindProperty] public string  EditQuestionText { get; set; } = "";
    [BindProperty] public string  EditOptionsJson  { get; set; } = "[]";
    [BindProperty] public int     EditCorrectIndex { get; set; }
    [BindProperty] public string? EditExplanation  { get; set; }
    [BindProperty] public int?    EditSubjectId     { get; set; }
    [BindProperty] public int?    EditTopicId       { get; set; }
    [BindProperty] public int?    EditDifficultyId  { get; set; }
    [BindProperty] public string? EditReviewNotes   { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            await LoadReferenceDataAsync();

            var query = _db.QuestionDrafts.AsQueryable();
            if (JobId.HasValue) query = query.Where(d => d.GenerationJobId == JobId.Value);

            var statusEnum = (DraftStatus)StatusTab;
            query = query.Where(d => d.Status == statusEnum);

            PendingCount  = await _db.QuestionDrafts.CountAsync(d => d.Status == DraftStatus.PendingReview);
            ApprovedCount = await _db.QuestionDrafts.CountAsync(d => d.Status == DraftStatus.Approved || d.Status == DraftStatus.EditedApproved);
            RejectedCount = await _db.QuestionDrafts.CountAsync(d => d.Status == DraftStatus.Rejected);

            TotalCount = await query.CountAsync();
            var raw = await query
                .OrderBy(d => d.Id)
                .Skip((PageNum - 1) * PageSize)
                .Take(PageSize)
                .Select(d => new
                {
                    d.Id, d.QuestionText, d.OptionsJson, d.CorrectIndex,
                    d.Explanation, d.FlagsJson, d.Status, d.DifficultyEstimate,
                    d.EstimatedSeconds, d.Model, d.CreatedAt, d.ReviewedAt,
                    d.SubjectId, d.TopicId, d.DifficultyLevelId, d.GenerationJobId
                })
                .ToListAsync();

            Drafts = raw.Select(d => new DraftRow(
                d.Id, d.QuestionText, d.OptionsJson, d.CorrectIndex,
                d.Explanation, d.FlagsJson, d.Status, d.DifficultyEstimate,
                d.EstimatedSeconds, d.Model ?? "", d.CreatedAt, d.ReviewedAt,
                d.SubjectId, d.TopicId, d.DifficultyLevelId, d.GenerationJobId
            )).ToList();
        }
        catch (Exception ex) when (IsSchemaNotReady(ex))
        {
            TablesReady = false;
        }
    }

    // ── Approve ────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostApproveAsync(int draftId)
    {
        try
        {
            var userId = GetUserId();
            await _review.ApproveAsync(draftId, userId);
            TempData["Success"] = $"Draft #{draftId} approved and published.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage(new { JobId, StatusTab, PageNum });
    }

    // ── Reject ─────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostRejectAsync(int draftId, string reason)
    {
        try
        {
            await _review.RejectAsync(draftId, GetUserId(), reason ?? "Rejected by reviewer");
            TempData["Success"] = $"Draft #{draftId} rejected.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage(new { JobId, StatusTab, PageNum });
    }

    // ── Edit + Approve ─────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostEditApproveAsync()
    {
        try
        {
            await _review.EditAndApproveAsync(
                EditDraftId, GetUserId(),
                EditQuestionText, EditOptionsJson, EditCorrectIndex,
                EditExplanation, EditSubjectId, EditTopicId, EditDifficultyId,
                EditReviewNotes);
            TempData["Success"] = $"Draft #{EditDraftId} edited and approved.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage(new { JobId, StatusTab, PageNum });
    }

    // ── Bulk Reject Job ────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostBulkRejectAsync(int jobId)
    {
        try
        {
            await _review.BulkRejectJobAsync(jobId, GetUserId());
            TempData["Success"] = $"All pending drafts for job #{jobId} rejected.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage(new { JobId, StatusTab, PageNum });
    }

    // ── Helpers ─────────────────────────────────────────────────────────────
    private Guid GetUserId()
    {
        var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return id is not null ? Guid.Parse(id) : Guid.Empty;
    }

    private async Task LoadReferenceDataAsync()
    {
        SubjectOptions = (await _db.Subjects
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, s.Name })
            .ToListAsync())
            .Select(s => (s.Id, s.Name)).ToList();

        TopicOptions = (await _db.Topics
            .OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.Name })
            .ToListAsync())
            .Select(t => (t.Id, t.Name)).ToList();

        DifficultyOptions = (await _db.DifficultyLevels
            .OrderBy(d => d.SortOrder)
            .Select(d => new { d.Id, d.Name })
            .ToListAsync())
            .Select(d => (d.Id, d.Name)).ToList();
    }

    // ── View model ──────────────────────────────────────────────────────────
    public record DraftRow(
        int Id, string QuestionText, string OptionsJson, int CorrectIndex,
        string? Explanation, string FlagsJson, DraftStatus Status,
        string? DifficultyEstimate, int? EstimatedSeconds, string Model,
        DateTime CreatedAt, DateTime? ReviewedAt,
        int? SubjectId, int? TopicId, int? DifficultyLevelId, int GenerationJobId);

    /// <summary>Parses the options JSON into typed records for the view.</summary>
    public static List<OptionItem> ParseOptions(string json)
    {
        try
        {
            var arr = JsonNode.Parse(json)?.AsArray();
            if (arr is null) return [];
            return arr.Select(o => new OptionItem(
                o?["label"]?.GetValue<string>() ?? "",
                o?["text"]?.GetValue<string>()  ?? "",
                o?["is_correct"]?.GetValue<bool>() ?? false
            )).ToList();
        }
        catch { return []; }
    }

    /// <summary>Parses flags JSON into a list of flag names.</summary>
    public static List<string> ParseFlags(string json)
    {
        try
        {
            var obj = JsonNode.Parse(json)?.AsObject();
            if (obj is null) return [];
            return obj.Where(kv => kv.Value?.GetValue<bool>() == true ||
                                    (kv.Value?.GetValueKind() == System.Text.Json.JsonValueKind.Object))
                      .Select(kv => kv.Key)
                      .ToList();
        }
        catch { return []; }
    }

    public record OptionItem(string Label, string Text, bool IsCorrect);

    private static bool IsSchemaNotReady(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
            if (e is Npgsql.PostgresException pg && pg.SqlState is "42P01" or "42703")
                return true;
        return false;
    }
}
