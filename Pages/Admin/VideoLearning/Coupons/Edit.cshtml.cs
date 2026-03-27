using GridAcademy.Common;
using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;
using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.Coupons;

[Authorize(Roles = "Admin")]
public class EditModel(ICouponService svc, IProgramService programSvc) : PageModel
{
    [BindProperty(SupportsGet = true)] public int Id { get; set; }

    [BindProperty] public string Code { get; set; } = "";
    [BindProperty] public string? Description { get; set; }
    [BindProperty] public DiscountType DiscountType { get; set; } = DiscountType.Percentage;
    [BindProperty] public decimal DiscountValue { get; set; }
    [BindProperty] public decimal? MaxDiscountInr { get; set; }
    [BindProperty] public decimal? MaxDiscountUsd { get; set; }
    [BindProperty] public DateTime? ValidFrom { get; set; }
    [BindProperty] public DateTime? ValidTo { get; set; }
    [BindProperty] public int? UsageLimit { get; set; }
    [BindProperty] public Guid? ProgramId { get; set; }
    [BindProperty] public bool IsActive { get; set; } = true;

    public bool IsEdit => Id > 0;
    public int CouponId => Id;

    public List<ProgramSummaryDto> Programs { get; set; } = [];

    private async Task LoadProgramsAsync()
    {
        var result = await programSvc.GetProgramsAsync(new ProgramListRequest(null, null, null, 1, 200));
        Programs = result.Items;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadProgramsAsync();
        if (IsEdit)
        {
            try
            {
                var c = await svc.GetByIdAsync(Id);
                Code = c.Code; Description = c.Description; DiscountType = c.DiscountType;
                DiscountValue = c.DiscountValue; MaxDiscountInr = c.MaxDiscountInr; MaxDiscountUsd = c.MaxDiscountUsd;
                ValidFrom = c.ValidFrom; ValidTo = c.ValidTo; UsageLimit = c.UsageLimit;
                ProgramId = c.ProgramId; IsActive = c.IsActive;
            }
            catch { return NotFound(); }
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadProgramsAsync();
        if (!ModelState.IsValid) return Page();
        try
        {
            if (IsEdit)
            {
                var req = new UpdateCouponRequest(Description, DiscountType, DiscountValue,
                    MaxDiscountInr, MaxDiscountUsd, ValidFrom, ValidTo, UsageLimit, ProgramId, IsActive);
                await svc.UpdateAsync(Id, req);
                TempData["Success"] = "Coupon updated.";
            }
            else
            {
                var req = new CreateCouponRequest(Code.ToUpperInvariant(), Description, DiscountType, DiscountValue,
                    MaxDiscountInr, MaxDiscountUsd, ValidFrom, ValidTo, UsageLimit, ProgramId, IsActive);
                await svc.CreateAsync(req);
                TempData["Success"] = "Coupon created.";
            }
            return RedirectToPage("Index");
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; return Page(); }
    }
}
