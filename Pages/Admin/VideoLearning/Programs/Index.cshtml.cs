using GridAcademy.Common;
using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;
using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.Programs;

[Authorize(Roles = "Admin,Instructor")]
public class IndexModel(IProgramService svc, IDomainService domainSvc) : PageModel
{
    [BindProperty(SupportsGet = true)] public int? DomainId { get; set; }
    [BindProperty(SupportsGet = true)] public ProgramStatus? Status { get; set; }
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; } = 1;

    public PagedResult<ProgramSummaryDto> Programs { get; set; } = new();
    public List<DomainDto> Domains { get; set; } = [];

    public ProgramListRequest FilterRequest => new(DomainId, Status, Search, CurrentPage, 20);

    public async Task OnGetAsync()
    {
        Domains = await domainSvc.GetAllAsync(activeOnly: false);
        Programs = await svc.GetProgramsAsync(FilterRequest);
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id)
    {
        try { await svc.PublishAsync(id); TempData["Success"] = "Program published."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnpublishAsync(Guid id)
    {
        try { await svc.UnpublishAsync(id); TempData["Success"] = "Program unpublished."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try { await svc.DeleteAsync(id); TempData["Success"] = "Program deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }
}
