using GridAcademy.DTOs.Assessment;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.Content.Tests;

[Authorize(Roles = "Admin,Instructor")]
public class AttemptDetailModel : PageModel
{
    private readonly IAssessmentService _assessment;
    public AttemptDetailModel(IAssessmentService assessment) => _assessment = assessment;

    public AttemptResultDto? Result { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid attemptId)
    {
        try   { Result = await _assessment.GetAttemptDetailAsync(attemptId); }
        catch { return NotFound(); }
        return Page();
    }
}
