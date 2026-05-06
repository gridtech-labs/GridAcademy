using System.Security.Claims;
using GridAcademy.DTOs.Assessment;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.Content.Tests;

[Authorize(Roles = "Admin,Instructor")]
public class CreateModel : PageModel
{
    // ── Default instructions shown in every new test (editable by admin) ──────
    // Shared inline-style helpers for question palette colour badges (used in DefaultInstructions)
    private const string _sq = "display:inline-block;width:22px;height:22px;vertical-align:middle;margin-right:8px;border-radius:3px;flex-shrink:0;";
    private const string _ci = "display:inline-block;width:22px;height:22px;vertical-align:middle;margin-right:8px;border-radius:50%;flex-shrink:0;";

    public static readonly string DefaultInstructions =
        $"""
        <ol style="line-height:1.9;padding-left:1.4rem;">
          <li>Total duration of this test is <strong>[Duration] min</strong>.</li>
          <li>The Questions Palette displayed on the right side of the screen will show the status of each question using one of the following colour codes:
            <ol style="list-style:none;padding-left:0.5rem;margin-top:0.5rem;">
              <li style="display:flex;align-items:center;margin-bottom:0.55rem;">
                <span style="{_sq}background:#fff;border:2px solid #adb5bd;"></span>
                You have <strong>&nbsp;not visited&nbsp;</strong> the question yet.
              </li>
              <li style="display:flex;align-items:center;margin-bottom:0.55rem;">
                <span style="{_sq}background:#e74c3c;"></span>
                You have <strong>&nbsp;not answered&nbsp;</strong> the question.
              </li>
              <li style="display:flex;align-items:center;margin-bottom:0.55rem;">
                <span style="{_sq}background:#27ae60;"></span>
                You have <strong>&nbsp;answered&nbsp;</strong> the question.
              </li>
              <li style="display:flex;align-items:center;margin-bottom:0.55rem;">
                <span style="{_ci}background:#8e44ad;"></span>
                You have <strong>&nbsp;NOT answered&nbsp;</strong> the question, but have <strong>marked it for review</strong>.
              </li>
              <li style="display:flex;align-items:center;margin-bottom:0.55rem;">
                <span style="position:relative;display:inline-block;width:22px;height:22px;vertical-align:middle;margin-right:8px;flex-shrink:0;">
                  <span style="display:block;width:22px;height:22px;background:#8e44ad;border-radius:50%;"></span>
                  <span style="position:absolute;bottom:-2px;right:-4px;width:11px;height:11px;background:#27ae60;border-radius:2px;border:1.5px solid #fff;"></span>
                </span>
                The question(s) <em>"Answered and Marked for Review"</em> will be considered for evaluation.
              </li>
            </ol>
          </li>
          <li>Clicking on <strong>"&gt;"</strong> will take you to the next question.</li>
          <li>Clicking on <strong>"&lt;"</strong> will take you to the previous question.</li>
          <li>Click on the question palette question number if you want to go directly to that question.</li>
          <li>Procedure for answering a multiple choice type question:
            <ol>
              <li>To select your answer, click on the button of your desired option.</li>
              <li>To deselect your chosen answer, click on the button of the chosen option again.</li>
              <li>To change your chosen answer, click on the button of another option.</li>
              <li>To save your answer, click the <strong>"&gt;"</strong> (Next) button.</li>
              <li>To mark the question for review, click <strong>[ MARK FOR REVIEW ]</strong> above the question.</li>
              <li>To unmark, click <strong>[ UNMARK FOR REVIEW ]</strong> again.</li>
            </ol>
          </li>
          <li>At the end, submit your exam by clicking the <strong>"SUBMIT EXAM"</strong> button.</li>
        </ol>
        """;

    private readonly ITestService _tests;

    public CreateModel(ITestService tests) => _tests = tests;

    public CreateTestRequest? Input { get; set; }

    public async Task OnGetAsync() { }

    public async Task<IActionResult> OnPostAsync(
        string title, string? instructions, int durationMinutes,
        decimal passingPercent, bool negativeMarkingEnabled)
    {
        if (!ModelState.IsValid) return Page();

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        Input = new CreateTestRequest
        {
            Title                  = title,
            Instructions           = instructions,
            DurationMinutes        = durationMinutes,
            PassingPercent         = passingPercent,
            NegativeMarkingEnabled = negativeMarkingEnabled
        };

        var test = await _tests.CreateTestAsync(Input, userId);
        TempData["Success"] = $"Test '{test.Title}' created. Now add sections.";
        return RedirectToPage("Edit", new { id = test.Id });
    }
}
