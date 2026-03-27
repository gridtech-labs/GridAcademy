using GridAcademy.DTOs.VideoLearning;
using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.LearningPaths;

[Authorize(Roles = "Admin,Instructor")]
public class IndexModel(ILearningPathService svc, IDomainService domainSvc) : PageModel
{
    [BindProperty(SupportsGet = true)] public int? DomainId { get; set; }

    public List<LearningPathDto> LearningPaths { get; set; } = [];
    public List<DomainDto> Domains { get; set; } = [];

    public async Task OnGetAsync()
    {
        Domains = await domainSvc.GetAllAsync(activeOnly: false);
        LearningPaths = DomainId.HasValue
            ? await svc.GetByDomainAsync(DomainId.Value, activeOnly: false)
            : await svc.GetAllAsync(activeOnly: false);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try { await svc.DeleteAsync(id); TempData["Success"] = "Learning path deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }
}
