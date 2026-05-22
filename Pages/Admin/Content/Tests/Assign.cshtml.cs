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
    public List<StudentItem> Students { get; set; } = [];
    public List<GroupItem>   Groups   { get; set; } = [];
    public List<TestAssignmentDto> Assignments { get; set; } = [];

    public bool IsSuperAdmin => User.IsInRole("SuperAdmin");

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ════════════════════════════════════════════════════════════════════════
    // GET
    // ════════════════════════════════════════════════════════════════════════
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        await LoadDataAsync(id);
        return Page();
    }

    // ════════════════════════════════════════════════════════════════════════
    // POST — assign to individual students
    // ════════════════════════════════════════════════════════════════════════
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

    // ════════════════════════════════════════════════════════════════════════
    // POST — assign to an entire group
    // ════════════════════════════════════════════════════════════════════════
    public async Task<IActionResult> OnPostAssignGroupAsync(
        Guid id, int groupId,
        DateTime availableFrom, DateTime availableTo, int maxAttempts)
    {
        if (groupId <= 0)
        {
            TempData["Error"] = "Please select a group.";
            return RedirectToPage(new { id });
        }

        try
        {
            var (assigned, skipped) = await _tests.AssignToGroupAsync(
                id, groupId,
                availableFrom.ToUniversalTime(),
                availableTo.ToUniversalTime(),
                maxAttempts,
                CurrentUserId);

            TempData["Success"] = skipped > 0
                ? $"Assigned to {assigned} member(s). {skipped} already had an assignment and were skipped."
                : $"Assigned to all {assigned} member(s) of the group.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }

        return RedirectToPage(new { id });
    }

    // ════════════════════════════════════════════════════════════════════════
    // POST — revoke a single assignment
    // ════════════════════════════════════════════════════════════════════════
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

    // ════════════════════════════════════════════════════════════════════════
    // Private helpers
    // ════════════════════════════════════════════════════════════════════════
    private async Task LoadDataAsync(Guid id)
    {
        Test = await _tests.GetTestByIdAsync(id);

        // Build client-scoped user & group queries
        var userQuery  = _db.Users.AsNoTracking().Where(u => u.Role == "User" && u.IsActive);
        var groupQuery = _db.Groups.AsNoTracking().Where(g => g.IsActive);

        if (!IsSuperAdmin)
        {
            var cidClaim = User.FindFirst("ClientId")?.Value;
            if (int.TryParse(cidClaim, out var cid))
            {
                userQuery  = userQuery.Where(u => u.ClientId == cid);
                groupQuery = groupQuery.Where(g => g.ClientId == cid);
            }
            else
            {
                userQuery  = userQuery.Where(_ => false);
                groupQuery = groupQuery.Where(_ => false);
            }
        }

        Students = await userQuery
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Select(u => new StudentItem(u.Id, u.FirstName + " " + u.LastName, u.Email))
            .ToListAsync();

        Groups = await groupQuery
            .OrderBy(g => g.Name)
            .Select(g => new GroupItem(g.Id, g.Name, g.UserGroups.Count))
            .ToListAsync();

        Assignments = await _tests.GetAssignmentsAsync(id);
    }

    public record StudentItem(Guid Id, string FullName, string Email);
    public record GroupItem(int Id, string Name, int MemberCount);
}
