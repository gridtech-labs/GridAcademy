using GridAcademy.DTOs.Assessment;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.Reports.StudentActivity;

[Authorize(Roles = "Admin,Instructor")]
public class DetailModel : PageModel
{
    private readonly IAssessmentService _assessment;
    public DetailModel(IAssessmentService assessment) => _assessment = assessment;

    public StudentActivityDetailDto? Student { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid studentId)
    {
        Student = await _assessment.GetStudentActivityDetailAsync(studentId);
        if (Student is null) return NotFound();
        return Page();
    }
}
