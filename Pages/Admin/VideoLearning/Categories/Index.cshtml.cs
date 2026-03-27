using GridAcademy.DTOs.VideoLearning;
using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.Categories;

[Authorize(Roles = "Admin,Instructor")]
public class IndexModel(IVideoCategoryService svc, IDomainService domainSvc) : PageModel
{
    [BindProperty(SupportsGet = true)] public int? DomainId { get; set; }

    public List<VideoCategoryDto> Categories { get; set; } = [];
    public List<DomainDto> Domains { get; set; } = [];

    public async Task OnGetAsync()
    {
        Domains = await domainSvc.GetAllAsync(activeOnly: false);
        Categories = DomainId.HasValue
            ? await svc.GetByDomainAsync(DomainId.Value, activeOnly: false)
            : await svc.GetAllAsync(activeOnly: false);
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try { await svc.DeleteAsync(id); TempData["Success"] = "Category deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }
}
