using GridAcademy.DTOs.VideoLearning;
using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.Domains;

[Authorize(Roles = "Admin,Instructor")]
public class IndexModel(IDomainService svc) : PageModel
{
    public List<DomainDto> Domains { get; set; } = [];

    public async Task OnGetAsync()
    {
        Domains = await svc.GetAllAsync(activeOnly: false);
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try { await svc.DeleteAsync(id); TempData["Success"] = "Domain deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }
}
