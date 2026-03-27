using GridAcademy.DTOs.Assessment;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace GridAcademy.Pages.Student;

[Authorize(Roles = "User")]
public class ResultModel : PageModel
{
    private readonly IAssessmentService _svc;

    public ResultModel(IAssessmentService svc)
    {
        _svc = svc;
    }

    [BindProperty(SupportsGet = true)]
    public Guid AttemptId { get; set; }

    public AttemptResultDto? Result { get; set; }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> OnGetAsync()
    {
        if (AttemptId == Guid.Empty)
        {
            TempData["Error"] = "Invalid attempt reference.";
            return RedirectToPage("/Student/Dashboard");
        }

        try
        {
            Result = await _svc.GetResultAsync(AttemptId, CurrentUserId);
        }
        catch (UnauthorizedAccessException)
        {
            TempData["Error"] = "You do not have access to this result.";
            return RedirectToPage("/Student/Dashboard");
        }
        catch (Exception)
        {
            TempData["Error"] = "Unable to load the result. The exam may still be in progress.";
            return RedirectToPage("/Student/Dashboard");
        }

        ViewData["Title"] = $"Result — {Result.TestTitle}";
        return Page();
    }
}
