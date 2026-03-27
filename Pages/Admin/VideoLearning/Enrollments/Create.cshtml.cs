using GridAcademy.DTOs.VideoLearning;
using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.Enrollments;

[Authorize(Roles = "Admin")]
public class CreateModel(IEnrollmentService svc, IProgramService programSvc) : PageModel
{
    [BindProperty] public Guid UserId { get; set; }
    [BindProperty] public Guid ProgramId { get; set; }
    [BindProperty] public int PricingPlanId { get; set; }
    [BindProperty] public decimal AmountPaidInr { get; set; }
    [BindProperty] public decimal AmountPaidUsd { get; set; }
    [BindProperty] public string? CouponCode { get; set; }
    [BindProperty] public decimal? DiscountApplied { get; set; }
    [BindProperty] public DateTime? ExpiresAt { get; set; }

    public List<ProgramSummaryDto> Programs { get; set; } = [];

    private async Task LoadLookupsAsync()
    {
        var progs = await programSvc.GetProgramsAsync(new ProgramListRequest(null, null, null, 1, 200));
        Programs = progs.Items;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadLookupsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadLookupsAsync();
        if (!ModelState.IsValid) return Page();
        try
        {
            var req = new CreateEnrollmentRequest(UserId, ProgramId, PricingPlanId,
                AmountPaidInr, AmountPaidUsd, CouponCode?.ToUpperInvariant(), DiscountApplied, null, ExpiresAt);
            await svc.EnrollAsync(req);
            TempData["Success"] = "User enrolled successfully.";
            return RedirectToPage("Index");
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; return Page(); }
    }
}
