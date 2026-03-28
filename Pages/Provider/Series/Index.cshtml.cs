using System.Security.Claims;
using GridAcademy.Common;
using GridAcademy.DTOs.Marketplace;
using GridAcademy.Services.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Provider.Series;

[Authorize(Roles = "Provider")]
public class IndexModel : PageModel
{
    private readonly IProviderService _providers;

    public IndexModel(IProviderService providers)
    {
        _providers = providers;
    }

    public PagedResult<TestSeriesListDto> Series { get; set; } = new();
    public int CurrentPage { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync(int page = 1, CancellationToken ct = default)
    {
        CurrentPage = page;
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            Series = await _providers.GetMySeriesAsync(userId, page, 20, ct);
        }
        catch (KeyNotFoundException)
        {
            TempData["Error"] = "Provider profile not found.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            await _providers.DeleteSeriesAsync(userId, id, ct);
            TempData["Success"] = "Series deleted successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSubmitAsync(Guid id, CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            await _providers.SubmitForReviewAsync(userId, id, ct);
            TempData["Success"] = "Series submitted for review. The admin team will review it shortly.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage();
    }
}
