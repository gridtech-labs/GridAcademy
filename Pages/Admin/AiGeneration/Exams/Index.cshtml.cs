using GridAcademy.Data;
using GridAcademy.Modules.AiGeneration.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Admin.AiGeneration.Exams;

[Authorize(Roles = "SuperAdmin,Admin,Instructor")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public List<ExamRow> ExamRows { get; private set; } = [];
    public bool TablesReady { get; private set; } = true;

    public async Task OnGetAsync()
    {
        try
        {
            // All exam pages joined with their AI config (if any)
            var query = _db.ExamPages
                .GroupJoin(
                    _db.AiExamConfigs,
                    ep => ep.Id,
                    cfg => cfg.ExamPageId,
                    (ep, cfgs) => new { ep, cfg = cfgs.FirstOrDefault() })
                .Where(x => string.IsNullOrEmpty(Search) || x.ep.Title.Contains(Search));

            var rows = await query
                .OrderBy(x => x.ep.Title)
                .Take(100)
                .Select(x => new ExamRow(
                    x.ep.Id,
                    x.ep.Title,
                    x.cfg != null ? x.cfg.Id     : (int?)null,
                    x.cfg != null ? x.cfg.IsAiEnabled : false,
                    x.cfg != null ? _db.AiExamSections.Count(s => s.AiExamConfigId == x.cfg.Id) : 0))
                .ToListAsync();

            ExamRows = rows;
        }
        catch (Exception ex) when (IsSchemaNotReady(ex))
        {
            TablesReady = false;
        }
    }

    public async Task<IActionResult> OnPostToggleAsync(Guid examPageId)
    {
        var cfg = await _db.AiExamConfigs.FirstOrDefaultAsync(c => c.ExamPageId == examPageId);
        if (cfg is null)
        {
            cfg = new AiExamConfig { ExamPageId = examPageId, IsAiEnabled = true };
            _db.AiExamConfigs.Add(cfg);
        }
        else
        {
            cfg.IsAiEnabled = !cfg.IsAiEnabled;
            cfg.UpdatedAt   = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        TempData["Success"] = cfg.IsAiEnabled ? "AI generation enabled." : "AI generation disabled.";
        return RedirectToPage();
    }

    public record ExamRow(Guid ExamPageId, string Title, int? ConfigId, bool IsEnabled, int SectionCount);

    private static bool IsSchemaNotReady(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
            if (e is Npgsql.PostgresException pg && pg.SqlState is "42P01" or "42703")
                return true;
        return false;
    }
}
