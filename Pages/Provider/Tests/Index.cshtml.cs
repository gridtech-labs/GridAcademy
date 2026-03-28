using System.Security.Claims;
using GridAcademy.Data;
using GridAcademy.Data.Entities.Assessment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Provider.Tests;

[Authorize(Roles = "Provider")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<TestListItem> Tests { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var tests = await _db.Tests
            .Include(t => t.ExamType)
            .Include(t => t.Sections)
            .Where(t => t.CreatedBy == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        Tests = tests.Select(t => new TestListItem(
            t.Id,
            t.Title,
            t.ExamType?.Name ?? "—",
            t.DurationMinutes,
            t.Status,
            t.Sections.Count
        )).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var test = await _db.Tests.FirstOrDefaultAsync(
            t => t.Id == id && t.CreatedBy == userId && t.Status == TestStatus.Draft, ct);

        if (test is null)
        {
            TempData["Error"] = "Test not found or cannot be deleted (only Draft tests can be deleted).";
            return RedirectToPage();
        }

        _db.Tests.Remove(test);
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Test deleted successfully.";
        return RedirectToPage();
    }

    public record TestListItem(
        Guid       Id,
        string     Title,
        string     ExamTypeName,
        int        DurationMinutes,
        TestStatus Status,
        int        SectionCount
    );
}
