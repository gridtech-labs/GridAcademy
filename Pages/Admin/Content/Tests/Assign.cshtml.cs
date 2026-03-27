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
public class AssignModel : PageModel
{
    private readonly ITestService _tests;
    private readonly AppDbContext _db;

    public AssignModel(ITestService tests, AppDbContext db)
    {
        _tests = tests;
        _db    = db;
    }

    public TestDetailDto? Test        { get; set; }
    public List<(Guid Id, string FullName, string Email)> Students { get; set; } = [];
    public List<TestAssignmentDto>    Assignments { get; set; } = [];

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task LoadDataAsync(Guid id)
    {
        Test = await _tests.GetTestByIdAsync(id);

        Students = await _db.Users
            .Where(u => u.Role == "User" && u.IsActive)
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName, u.Email })
            .ToListAsync()
            .ContinueWith(t => t.Result.Select(u => (u.Id, u.FullName, u.Email)).ToList());

        Assignments = await _tests.GetAssignmentsAsync(id);
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        await LoadDataAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAssignAsync(
        Guid id, List<Guid> studentIds,
        DateTime availableFrom, DateTime availableTo, int maxAttempts)
    {
        if (!studentIds.Any())
        {
            TempData["Error"] = "Select at least one student.";
            await LoadDataAsync(id);
            return Page();
        }

        try
        {
            var assigned = await _tests.AssignTestAsync(id, new AssignTestRequest
            {
                StudentIds    = studentIds,
                AvailableFrom = availableFrom.ToUniversalTime(),
                AvailableTo   = availableTo.ToUniversalTime(),
                MaxAttempts   = maxAttempts
            }, CurrentUserId);

            TempData["Success"] = $"Assigned to {assigned.Count} student(s).";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRevokeAsync(Guid id, Guid assignmentId)
    {
        try
        {
            await _tests.RevokeAssignmentAsync(assignmentId);
            TempData["Success"] = "Assignment revoked.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }

        return RedirectToPage(new { id });
    }
}
