using System.Security.Claims;
using GridAcademy.DTOs.Assessment;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.Content.Tests;

[Authorize(Roles = "Admin,Instructor")]
public class IndexModel : PageModel
{
    private readonly ITestService _tests;
    public IndexModel(ITestService tests) => _tests = tests;

    public List<TestListDto> Tests       { get; set; } = [];
    public string?           Search      { get; set; }
    public int?              StatusFilter { get; set; }

    public async Task OnGetAsync(string? search, int? status)
    {
        Search       = search;
        StatusFilter = status;
        Tests = await _tests.GetTestsAsync(new TestListRequest
        {
            Search = search,
            Status = status.HasValue
                ? (GridAcademy.Data.Entities.Assessment.TestStatus)status.Value
                : null,
            PageSize = 100
        });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _tests.DeleteTestAsync(id);
            TempData["Success"] = "Test deleted.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage();
    }
}
