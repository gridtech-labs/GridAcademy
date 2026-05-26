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
            // Fetch each set independently — avoids complex GroupJoin/correlated-subquery
            // translation issues across EF Core versions.
            var examPages = await _db.ExamPages
                .Where(ep => string.IsNullOrEmpty(Search) || ep.Title.Contains(Search))
                .OrderBy(ep => ep.Title)
                .Take(100)
                .Select(ep => new { ep.Id, ep.Title })
                .ToListAsync();

            var configs = await _db.AiExamConfigs
                .Select(c => new { c.Id, c.ExamPageId, c.IsAiEnabled })
                .ToListAsync();

            var sectionCounts = await _db.AiExamSections
                .GroupBy(s => s.AiExamConfigId)
                .Select(g => new { ConfigId = g.Key, Count = g.Count() })
                .ToListAsync();

            var configByPage    = configs.ToDictionary(c => c.ExamPageId);
            var countByConfigId = sectionCounts.ToDictionary(s => s.ConfigId, s => s.Count);

            ExamRows = examPages.Select(ep =>
            {
                configByPage.TryGetValue(ep.Id, out var cfg);
                var sections = cfg is not null && countByConfigId.TryGetValue(cfg.Id, out var n) ? n : 0;
                return new ExamRow(ep.Id, ep.Title, cfg?.Id, cfg?.IsAiEnabled ?? false, sections);
            }).ToList();
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
