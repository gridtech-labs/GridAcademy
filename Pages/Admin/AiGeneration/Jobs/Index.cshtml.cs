using GridAcademy.Data;
using GridAcademy.Modules.AiGeneration.Domain.Entities;
using GridAcademy.Modules.AiGeneration.Jobs;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Admin.AiGeneration.Jobs;

[Authorize(Roles = "SuperAdmin,Admin,Instructor")]
public class IndexModel : PageModel
{
    private readonly AppDbContext       _db;
    private readonly IBackgroundJobClient _hangfire;
    public IndexModel(AppDbContext db, IBackgroundJobClient hangfire)
    {
        _db       = db;
        _hangfire = hangfire;
    }

    // ── Filters ────────────────────────────────────────────────────────────
    [BindProperty(SupportsGet = true)] public int? StatusFilter { get; set; }
    [BindProperty(SupportsGet = true)] public int  PageNum      { get; set; } = 1;
    private const int PageSize = 20;

    public int TotalCount { get; private set; }
    public List<JobRow> Jobs { get; private set; } = [];
    public bool TablesReady { get; private set; } = true;

    // ── "New Job" form ─────────────────────────────────────────────────────
    [BindProperty] public Guid?  SelectedExamPageId    { get; set; }
    [BindProperty] public int?   SelectedSectionId     { get; set; }
    [BindProperty] public int?   SelectedTopicId       { get; set; }
    [BindProperty] public string Difficulty            { get; set; } = "medium";
    [BindProperty] public string Language              { get; set; } = "en";
    [BindProperty] public int    Count                 { get; set; } = 10;
    [BindProperty] public string? Notes                { get; set; }

    // ── Reference lists ───────────────────────────────────────────────────
    public List<(Guid Id, string Title)>  ExamOptions    { get; private set; } = [];
    public List<(int Id, string Name)>    SectionOptions { get; private set; } = [];

    public async Task OnGetAsync()
    {
        try
        {
            await LoadReferenceDataAsync();

            var query = _db.GenerationJobs.AsQueryable();
            if (StatusFilter.HasValue)
                query = query.Where(j => (int)j.Status == StatusFilter.Value);

            TotalCount = await query.CountAsync();
            Jobs = await query
                .OrderByDescending(j => j.CreatedAt)
                .Skip((PageNum - 1) * PageSize)
                .Take(PageSize)
                .Select(j => new JobRow(
                    j.Id, j.Difficulty, j.Language, j.Count, j.Generated, j.AutoFlagged,
                    j.Status, j.ErrorMessage, j.CreatedAt, j.CompletedAt))
                .ToListAsync();
        }
        catch (Exception ex) when (IsSchemaNotReady(ex))
        {
            TablesReady = false;
        }
    }

    public async Task<IActionResult> OnPostEnqueueAsync()
    {
        if (!SelectedExamPageId.HasValue || Count < 1)
        {
            TempData["Error"] = "Select an exam and a valid count.";
            return RedirectToPage();
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var job = new GenerationJob
        {
            ExamPageId       = SelectedExamPageId.Value,
            AiExamSectionId  = SelectedSectionId,
            AiExamTopicId    = SelectedTopicId,
            Difficulty       = Difficulty,
            Language         = Language,
            Count            = Count,
            Notes            = Notes,
            Status           = GenerationJobStatus.Queued,
            CreatedAt        = DateTime.UtcNow,
            CreatedBy        = userId is not null ? Guid.Parse(userId) : null
        };
        _db.GenerationJobs.Add(job);
        await _db.SaveChangesAsync();

        // Enqueue Hangfire job
        var hangfireId = _hangfire.Enqueue<GenerationWorkerJob>(
            w => w.ExecuteAsync(job.Id, CancellationToken.None));

        job.HangfireJobId = hangfireId;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Job #{job.Id} queued — Hangfire ID: {hangfireId}";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAsync(int jobId)
    {
        var job = await _db.GenerationJobs.FindAsync(jobId);
        if (job is not null && job.Status is GenerationJobStatus.Queued or GenerationJobStatus.Running)
        {
            if (!string.IsNullOrEmpty(job.HangfireJobId))
                BackgroundJob.Delete(job.HangfireJobId);

            job.Status      = GenerationJobStatus.Cancelled;
            job.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Job #{jobId} cancelled.";
        }
        return RedirectToPage();
    }

    private async Task LoadReferenceDataAsync()
    {
        ExamOptions = await _db.AiExamConfigs
            .Where(c => c.IsAiEnabled)
            .Join(_db.ExamPages, c => c.ExamPageId, ep => ep.Id,
                (c, ep) => new ValueTuple<Guid, string>(ep.Id, ep.Title))
            .OrderBy(x => x.Item2)
            .ToListAsync();

        SectionOptions = await _db.AiExamSections
            .Where(s => ExamOptions.Select(e => e.Id).Contains(
                _db.AiExamConfigs.Where(c => c.Id == s.AiExamConfigId).Select(c => c.ExamPageId).FirstOrDefault()))
            .OrderBy(s => s.SectionName)
            .Select(s => new ValueTuple<int, string>(s.Id, s.SectionName))
            .ToListAsync();
    }

    public record JobRow(
        int Id, string Difficulty, string Language,
        int Count, int Generated, int AutoFlagged,
        GenerationJobStatus Status, string? ErrorMessage,
        DateTime CreatedAt, DateTime? CompletedAt);

    private static bool IsSchemaNotReady(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
            if (e is Npgsql.PostgresException pg && pg.SqlState is "42P01" or "42703")
                return true;
        return false;
    }
}
