using GridAcademy.Data;
using GridAcademy.Data.Entities.Payment;
using GridAcademy.DTOs.Payment;
using GridAcademy.Services.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Admin.Exams.Payments;

[Authorize(Roles = "Admin")]
public class OffersModel(IExamOfferService offerSvc, AppDbContext db) : PageModel
{
    public List<ExamOfferDto> Offers { get; set; } = [];

    // Exam list for the dropdown
    public List<(Guid Id, string Title)> ExamPages { get; set; } = [];

    [BindProperty] public SaveExamOfferRequest? Form { get; set; }
    [BindProperty] public int? EditId { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (Form == null || !ModelState.IsValid) { await LoadAsync(); return Page(); }
        try
        {
            await offerSvc.SaveAsync(EditId, Form);
            TempData["Success"] = EditId.HasValue ? "Offer updated." : "Offer created.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try { await offerSvc.DeleteAsync(id); TempData["Success"] = "Offer deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Offers    = await offerSvc.GetAllOffersAsync();
        ExamPages = await db.ExamPages.AsNoTracking()
            .OrderBy(e => e.Title)
            .Select(e => new { e.Id, e.Title })
            .ToListAsync()
            .ContinueWith(t => t.Result.Select(e => (e.Id, e.Title)).ToList());
    }
}
