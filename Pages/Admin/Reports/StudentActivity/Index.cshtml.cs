using GridAcademy.Common;
using GridAcademy.DTOs.Assessment;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.Reports.StudentActivity;

[Authorize(Roles = "Admin,Instructor")]
public class IndexModel : PageModel
{
    private readonly IAssessmentService _assessment;
    public IndexModel(IAssessmentService assessment) => _assessment = assessment;

    public PagedResult<StudentActivitySummaryDto> Students { get; set; } = new();

    [BindProperty(SupportsGet = true)] public string? Search      { get; set; }
    [BindProperty(SupportsGet = true)] public int     CurrentPage { get; set; } = 1;

    private const int PageSize = 15;

    public async Task OnGetAsync()
    {
        Students = await _assessment.GetStudentActivityAsync(Search, CurrentPage, PageSize);
    }
}
