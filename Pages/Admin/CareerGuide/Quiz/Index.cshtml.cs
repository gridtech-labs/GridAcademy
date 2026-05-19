using GridAcademy.DTOs.CareerGuide;
using GridAcademy.Services.CareerGuide;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.CareerGuide.Quiz;

[Authorize]
public class IndexModel(ICareerGuideService svc) : PageModel
{
    public List<CareerQuizQuestionDto> Questions { get; private set; } = [];

    // ── Edit form bindings ─────────────────────────────────────────────────
    [BindProperty] public string  QText    { get; set; } = "";
    [BindProperty] public int     QSort    { get; set; }
    [BindProperty] public bool    QActive  { get; set; } = true;
    [BindProperty] public int     EditId   { get; set; }   // 0 = new
    // Options come as parallel arrays from the form
    [BindProperty] public List<string> OptText     { get; set; } = [];
    [BindProperty] public List<string> OptCategory { get; set; } = [];

    public async Task OnGetAsync() =>
        Questions = await svc.GetAllQuestionsAsync();

    // ── Save (create or update) ──────────────────────────────────────────────
    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (string.IsNullOrWhiteSpace(QText))
        {
            TempData["Error"] = "Question text is required.";
            Questions = await svc.GetAllQuestionsAsync();
            return Page();
        }

        var options = OptText
            .Select((t, i) => new CreateOptionRequest(t, OptCategory.ElementAtOrDefault(i) ?? "", i))
            .Where(o => !string.IsNullOrWhiteSpace(o.OptionText))
            .ToList();

        if (options.Count < 2)
        {
            TempData["Error"] = "Please add at least 2 options.";
            Questions = await svc.GetAllQuestionsAsync();
            return Page();
        }

        if (EditId == 0)
        {
            await svc.CreateQuestionAsync(new CreateQuizQuestionRequest(QText, QSort, QActive, options));
            TempData["Success"] = "Question created successfully.";
        }
        else
        {
            await svc.UpdateQuestionAsync(EditId, new UpdateQuizQuestionRequest(QText, QSort, QActive, options));
            TempData["Success"] = "Question updated successfully.";
        }

        return RedirectToPage();
    }

    // ── Delete ───────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await svc.DeleteQuestionAsync(id);
        TempData["Success"] = "Question deleted.";
        return RedirectToPage();
    }

    // ── Toggle active ────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        await svc.ToggleActiveAsync(id);
        return RedirectToPage();
    }
}
