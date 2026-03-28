using System.Security.Claims;
using GridAcademy.DTOs.Marketplace;
using GridAcademy.Services.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Provider;

[Authorize(Roles = "Provider")]
public class DashboardModel : PageModel
{
    private readonly IProviderService _providers;

    public DashboardModel(IProviderService providers)
    {
        _providers = providers;
    }

    public ProviderDashboardDto? Stats    { get; set; }
    public ProviderDto?          Profile  { get; set; }
    public string?               StatusWarning { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            Profile = await _providers.GetProfileAsync(userId, ct);
            Stats   = await _providers.GetDashboardAsync(userId, ct);

            if (Profile.Status == "Pending")
                StatusWarning = "Your account is pending admin verification. You can create content but cannot publish series until verified.";
            else if (Profile.Status == "Suspended")
                StatusWarning = "Your account has been suspended. Please contact support for assistance.";
        }
        catch (KeyNotFoundException)
        {
            TempData["Error"] = "Provider profile not found. Please contact support.";
        }

        return Page();
    }
}
