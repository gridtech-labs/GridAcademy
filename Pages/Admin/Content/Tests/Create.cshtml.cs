using System.Security.Claims;
using GridAcademy.Data;
using GridAcademy.DTOs.Assessment;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Admin.Content.Tests;

[Authorize(Roles = "Admin,Instructor")]
public class CreateModel : PageModel
{
    // ── Default instructions shown in every new test (editable by admin) ──────
    public static readonly string DefaultInstructions =
        """
        <ol>
          <li>Total duration of this test is <strong>[Duration] min</strong>.</li>
          <li>The Questions Palette displayed on the right side of the screen will show the status of each question using one of the following symbols:
            <ol>
              <li>You have <strong>not visited</strong> the question yet.</li>
              <li>You have <strong>not answered</strong> the question.</li>
              <li>You have <strong>answered</strong> the question.</li>
              <li>You have <strong>NOT answered</strong> the question, but have <strong>marked the question for review</strong>.</li>
              <li>The question(s) <em>"Answered and Marked for Review"</em> will be considered for evaluation.</li>
            </ol>
          </li>
          <li>Clicking on <strong>"&gt;"</strong> will take you to the next question.</li>
          <li>Clicking on <strong>"&lt;"</strong> will take you to the previous question.</li>
          <li>Click on the question palette question number if you want to go directly to that question.</li>
          <li>Procedure for answering a multiple choice question:
            <ol>
              <li>To select your answer, click the button of your desired answer.</li>
              <li>To deselect your chosen answer, click on the button of the chosen option again.</li>
              <li>To change your chosen answer, click on the button of another option.</li>
              <li>To save your answer, go to the <strong>"&gt;"</strong> button which appears on the right side at the top of your question.</li>
              <li>To mark the question for review, click on the bar given above the question. <strong>[ MARK FOR REVIEW ]</strong></li>
              <li>To unmark the question (that is marked for review), click again on the bar given above the question. <strong>[ UNMARK FOR REVIEW ]</strong></li>
            </ol>
          </li>
          <li>At the end, submit your exam by clicking on the <strong>"SUBMIT EXAM"</strong> button.</li>
        </ol>
        """;

    private readonly ITestService _tests;
    private readonly AppDbContext _db;

    public CreateModel(ITestService tests, AppDbContext db)
    {
        _tests = tests;
        _db    = db;
    }

    public CreateTestRequest? Input     { get; set; }
    public List<(int Id, string Name)> ExamTypes { get; set; } = [];

    public async Task OnGetAsync()
    {
        ExamTypes = await _db.ExamTypes
            .Where(e => e.IsActive)
            .Select(e => new { e.Id, e.Name })
            .ToListAsync()
            .ContinueWith(t => t.Result.Select(e => (e.Id, e.Name)).ToList());
    }

    public async Task<IActionResult> OnPostAsync(
        string title, string? instructions, int durationMinutes,
        decimal passingPercent, int examTypeId, bool negativeMarkingEnabled)
    {
        ExamTypes = await _db.ExamTypes
            .Where(e => e.IsActive)
            .Select(e => new { e.Id, e.Name })
            .ToListAsync()
            .ContinueWith(t => t.Result.Select(e => (e.Id, e.Name)).ToList());

        if (!ModelState.IsValid) return Page();

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        Input = new CreateTestRequest
        {
            Title                  = title,
            Instructions           = instructions,
            DurationMinutes        = durationMinutes,
            PassingPercent         = passingPercent,
            ExamTypeId             = examTypeId,
            NegativeMarkingEnabled = negativeMarkingEnabled
        };

        var test = await _tests.CreateTestAsync(Input, userId);
        TempData["Success"] = $"Test '{test.Title}' created. Now add sections.";
        return RedirectToPage("Edit", new { id = test.Id });
    }
}
