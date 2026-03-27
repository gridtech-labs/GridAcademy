using GridAcademy.DTOs.Assessment;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.Content.Tests;

[Authorize(Roles = "Admin,Instructor")]
public class ResultsModel : PageModel
{
    private readonly ITestService       _tests;
    private readonly IAssessmentService _assessment;

    public ResultsModel(ITestService tests, IAssessmentService assessment)
    {
        _tests      = tests;
        _assessment = assessment;
    }

    public TestDetailDto?            Test     { get; set; }
    public List<AttemptSummaryDto>   Attempts { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try   { Test = await _tests.GetTestByIdAsync(id); }
        catch { return NotFound(); }

        Attempts = await _assessment.GetAttemptsByTestAsync(id);
        return Page();
    }
}
