using System.Security.Claims;
using GridAcademy.DTOs.Assessment;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Student;

[Authorize(Roles = "User")]
public class PerformanceModel : PageModel
{
    private readonly IAssessmentService _assessment;

    public PerformanceModel(IAssessmentService assessment)
    {
        _assessment = assessment;
    }

    public MyPerformanceDto Performance { get; set; } = new();

    public async Task OnGetAsync()
    {
        var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Performance = await _assessment.GetMyPerformanceAsync(studentId);
    }

    public static string FormatDuration(int seconds)
    {
        var m = seconds / 60;
        var s = seconds % 60;
        if (m >= 60) return $"{m / 60}h {m % 60}m";
        return m > 0 ? $"{m}m {s}s" : $"{s}s";
    }
}
