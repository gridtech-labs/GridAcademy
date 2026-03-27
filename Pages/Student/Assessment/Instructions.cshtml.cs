using System.Security.Claims;
using GridAcademy.DTOs.Assessment;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Student.Assessment;

[Authorize(Roles = "User")]
public class InstructionsModel : PageModel
{
    private readonly ITestService       _tests;
    private readonly IAssessmentService _assessment;

    public InstructionsModel(ITestService tests, IAssessmentService assessment)
    {
        _tests      = tests;
        _assessment = assessment;
    }

    public TestDetailDto?  Test          { get; set; }
    public Guid            AssignmentId  { get; set; }
    public int             AttemptsUsed  { get; set; }
    public int             MaxAttempts   { get; set; }
    public DateTime        AvailableTo   { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid assignmentId)
    {
        var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Resolve assignment → test
        var cards = await _assessment.GetAvailableTestsAsync(studentId);
        var card  = cards.FirstOrDefault(c => c.AssignmentId == assignmentId);
        if (card == null) return NotFound();

        // If there's an in-progress attempt, redirect straight to Take
        if (card.HasInProgressAttempt && card.InProgressAttemptId.HasValue)
            return RedirectToPage("/Student/Assessment/Take", new { attemptId = card.InProgressAttemptId });

        Test         = await _tests.GetTestByIdAsync(card.TestId);
        AssignmentId = assignmentId;
        AttemptsUsed = card.AttemptsUsed;
        MaxAttempts  = card.MaxAttempts;
        AvailableTo  = card.AvailableTo;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid assignmentId)
    {
        var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var attempt = await _assessment.StartAttemptAsync(assignmentId, studentId);
            return RedirectToPage("/Student/Assessment/Take", new { attemptId = attempt.AttemptId });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Student/Dashboard");
        }
    }
}
