using System.Security.Claims;
using GridAcademy.DTOs.Assessment;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Student;

[Authorize(Roles = "User")]
public class DashboardModel : PageModel
{
    private readonly IAssessmentService _assessment;

    public DashboardModel(IAssessmentService assessment)
    {
        _assessment = assessment;
    }

    public List<StudentTestCardDto> Tests { get; set; } = [];

    public async Task OnGetAsync()
    {
        var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Tests = await _assessment.GetAvailableTestsAsync(studentId);
    }
}
