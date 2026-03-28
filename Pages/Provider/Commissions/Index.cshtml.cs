using System.Security.Claims;
using GridAcademy.Data.Entities.Marketplace;
using GridAcademy.DTOs.Marketplace;
using GridAcademy.Services.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Provider.Commissions;

[Authorize(Roles = "Provider")]
public class IndexModel : PageModel
{
    private readonly IProviderService _providers;

    public IndexModel(IProviderService providers)
    {
        _providers = providers;
    }

    public IReadOnlyList<CommissionDto> Commissions  { get; set; } = [];
    public decimal                      TotalEarned  { get; set; }
    public decimal                      PendingPayout { get; set; }
    public decimal                      ProcessedPayout { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            Commissions = await _providers.GetCommissionsAsync(userId, ct);

            TotalEarned     = Commissions.Sum(c => c.ProviderAmount);
            PendingPayout   = Commissions.Where(c => c.Status == CommissionStatus.Pending).Sum(c => c.ProviderAmount);
            ProcessedPayout = Commissions.Where(c => c.Status == CommissionStatus.Processed).Sum(c => c.ProviderAmount);
        }
        catch (KeyNotFoundException)
        {
            TempData["Error"] = "Provider profile not found.";
        }

        return Page();
    }
}
