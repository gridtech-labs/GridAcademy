using GridAcademy.Common;
using GridAcademy.DTOs.VideoLearning;
using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.Coupons;

[Authorize(Roles = "Admin")]
public class IndexModel(ICouponService svc) : PageModel
{
    [BindProperty(SupportsGet = true)] public bool? IsActive { get; set; }
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; } = 1;

    public PagedResult<CouponDto> Coupons { get; set; } = new();

    public async Task OnGetAsync()
    {
        Coupons = await svc.GetCouponsAsync(new CouponListRequest(null, IsActive, Search, CurrentPage, 20));
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try { await svc.DeleteAsync(id); TempData["Success"] = "Coupon deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }
}
