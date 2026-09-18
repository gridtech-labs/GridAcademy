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
    /// <summary>
    /// The standard instructions, shared with TestService and the DbSeeder backfill so every
    /// test carries the same text. Edit them in one place: Helpers/DefaultTestInstructions.
    /// </summary>
    public static readonly string DefaultInstructions = Helpers.DefaultTestInstructions.Html;

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
