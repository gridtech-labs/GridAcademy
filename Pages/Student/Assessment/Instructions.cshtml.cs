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

    /// <summary>
    /// If the student has an in-progress attempt that has already had answers saved,
    /// we redirect directly to Take. If the attempt is fresh (no answers yet), we
    /// show Instructions and let the student acknowledge before entering the exam.
    /// This property holds the in-progress attempt ID for the "resume" case.
    /// </summary>
    public Guid? InProgressAttemptId { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid assignmentId)
    {
        var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Resolve assignment → test
        var cards = await _assessment.GetAvailableTestsAsync(studentId);
        var card  = cards.FirstOrDefault(c => c.AssignmentId == assignmentId);
        if (card == null) return NotFound();

        // Check for in-progress attempt
        if (card.HasInProgressAttempt && card.InProgressAttemptId.HasValue)
        {
            var attemptId = card.InProgressAttemptId.Value;

            // If the student already has saved answers, they are mid-test — go directly to Take
            var hasAnswers = await _db.AttemptAnswers
                .AnyAsync(a => a.AttemptId == attemptId && !a.IsClear
                    && (a.SelectedOptionIds != null || a.NumericalValue != null));
            if (hasAnswers)
                return RedirectToPage("/Student/Assessment/Take", new { attemptId });

            // Fresh attempt (0 saved answers) — show Instructions so student can acknowledge
            InProgressAttemptId = attemptId;
        }

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
                .Where(tq => tq.TestId == Test.Id && tq.SectionId.HasValue && sectionIds.Contains(tq.SectionId.Value))
                .GroupBy(tq => tq.SectionId!.Value)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            // Questions added before sections were created land with SectionId = null.
            // Roll them into the first section so the count is never hidden.
            var unassignedCount = await _db.TestQuestions
                .CountAsync(tq => tq.TestId == Test.Id && tq.SectionId == null);
            if (unassignedCount > 0)
            {
                var firstSectionId = Test.Sections.OrderBy(s => s.SortOrder).ThenBy(s => s.Id).First().Id;
                ManualSectionCounts[firstSectionId] =
                    ManualSectionCounts.GetValueOrDefault(firstSectionId) + unassignedCount;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid assignmentId, Guid? inProgressAttemptId)
    {
        var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            Guid attemptId;

            if (inProgressAttemptId.HasValue && inProgressAttemptId.Value != Guid.Empty)
            {
                // Already have a fresh in-progress attempt — resume it instead of creating a duplicate
                attemptId = inProgressAttemptId.Value;
            }
            else
            {
                var attempt = await _assessment.StartAttemptAsync(assignmentId, studentId);
                attemptId = attempt.AttemptId;
            }

            // ── Mark instructions as acknowledged ─────────────────────────────
            // This flag lets the Take page know the student has read and agreed to
            // the instructions, regardless of how the attempt was originally created.
            await _db.TestAttempts
                .Where(a => a.Id == attemptId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.InstructionsAcknowledged, true));

            return RedirectToPage("/Student/Assessment/Take", new { attemptId });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Student/Dashboard");
        }
    }
}
