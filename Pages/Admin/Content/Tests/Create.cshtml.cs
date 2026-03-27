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
