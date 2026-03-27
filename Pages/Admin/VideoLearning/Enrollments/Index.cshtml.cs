using GridAcademy.Common;
using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;
using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.Enrollments;

[Authorize(Roles = "Admin")]
public class IndexModel(IEnrollmentService svc) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? UserEmail { get; set; }
    [BindProperty(SupportsGet = true)] public EnrollmentStatus? Status { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }
    [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; } = 1;

    public PagedResult<EnrollmentDto> Enrollments { get; set; } = new();

    public EnrollmentListRequest FilterRequest => new(null, null, Status, UserEmail, FromDate, ToDate, CurrentPage, 20);

    public async Task OnGetAsync()
    {
        Enrollments = await svc.GetEnrollmentsAsync(FilterRequest);
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id)
    {
        try { await svc.CancelAsync(id); TempData["Success"] = "Enrollment cancelled."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }
}
