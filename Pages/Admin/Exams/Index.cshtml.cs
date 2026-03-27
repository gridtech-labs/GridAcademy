using GridAcademy.DTOs.Exam;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.Exams;

[Authorize(Roles = "Admin,Instructor")]
public class IndexModel(IExamService svc) : PageModel
{
    [BindProperty(SupportsGet = true)] public int?    LevelId { get; set; }
    [BindProperty(SupportsGet = true)] public string? Search  { get; set; }

    public List<ExamPageCardDto> Exams  { get; set; } = [];
    public List<ExamLevelDto>    Levels { get; set; } = [];

    public async Task OnGetAsync()
    {
        Levels = await svc.GetExamLevelsAsync();
        Exams  = await svc.GetExamPagesAsync(activeOnly: false, LevelId, Search);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try   { await svc.DeleteExamAsync(id); TempData["Success"] = "Exam deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }
}
