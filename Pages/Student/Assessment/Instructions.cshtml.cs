using System.Security.Claims;
using GridAcademy.Data;
using GridAcademy.Data.Entities.Assessment;
using GridAcademy.DTOs.Assessment;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Student.Assessment;

[Authorize(Roles = "User")]
public class InstructionsModel : PageModel
{
    private readonly ITestService       _tests;
    private readonly IAssessmentService _assessment;
    private readonly AppDbContext       _db;

    public InstructionsModel(ITestService tests, IAssessmentService assessment, AppDbContext db)
    {
        _tests      = tests;
        _assessment = assessment;
        _db         = db;
    }

    public TestDetailDto?  Test          { get; set; }
    public Guid            AssignmentId  { get; set; }
    public int             AttemptsUsed  { get; set; }
    public int             MaxAttempts   { get; set; }
    public DateTime        AvailableTo   { get; set; }

    /// <summary>True when the test uses Manual (explicit) question assignment.</summary>
    public bool IsManualMode { get; set; }

    /// <summary>
    /// For Manual-mode tests: actual assigned question count per section ID.
    /// Keyed by TestSection.Id → number of questions in test_questions for that section.
    /// </summary>
    public Dictionary<int, int> ManualSectionCounts { get; set; } = [];

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

        // For Manual-mode tests, load real question counts from test_questions
        IsManualMode = Test.QuestionMode == QuestionMode.Manual;
        if (IsManualMode && Test.Sections.Count > 0)
        {
            var sectionIds = Test.Sections.Select(s => s.Id).ToList();
            ManualSectionCounts = await _db.TestQuestions
                .Where(tq => tq.SectionId.HasValue && sectionIds.Contains(tq.SectionId.Value))
                .GroupBy(tq => tq.SectionId!.Value)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

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
