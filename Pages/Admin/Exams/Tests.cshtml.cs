using GridAcademy.Data;
using GridAcademy.Data.Entities.Assessment;
using GridAcademy.DTOs.Exam;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Admin.Exams;

[Authorize(Roles = "Admin,Instructor")]
public class TestsModel(IExamService svc, AppDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }

    public ExamPageDetailDto? Exam     { get; set; }
    public List<ExamTestDto>  Mapped   { get; set; } = [];
    public List<Test>         AllTests { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        Exam = await svc.GetExamByIdAsync(Id);
        if (Exam == null) return NotFound();
        Mapped = await svc.GetMappedTestsAsync(Id);
        var mappedIds = Mapped.Select(m => m.TestId).ToHashSet();
        AllTests = await db.Tests
            .Where(t => t.Status == TestStatus.Published && !mappedIds.Contains(t.Id))
            .OrderBy(t => t.Title).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostMapAsync(Guid testId, bool isFree = true)
    {
        try
        {
            await svc.MapTestAsync(Id, new MapTestRequest(testId, isFree));
            TempData["Success"] = "Test added to exam.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostUnmapAsync(Guid testId)
    {
        await svc.UnmapTestAsync(Id, testId);
        TempData["Success"] = "Test removed.";
        return RedirectToPage(new { id = Id });
    }
}
