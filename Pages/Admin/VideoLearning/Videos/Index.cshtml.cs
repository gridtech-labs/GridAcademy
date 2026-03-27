using GridAcademy.Common;
using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;
using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.Videos;

[Authorize(Roles = "Admin,Instructor")]
public class IndexModel(IVideoService svc, IDomainService domainSvc, IVideoCategoryService catSvc) : PageModel
{
    [BindProperty(SupportsGet = true)] public int? DomainId { get; set; }
    [BindProperty(SupportsGet = true)] public int? CategoryId { get; set; }
    [BindProperty(SupportsGet = true)] public VideoStatus? Status { get; set; }
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; } = 1;

    public PagedResult<VideoDto> Videos { get; set; } = new();
    public List<DomainDto> Domains { get; set; } = [];
    public List<VideoCategoryDto> Categories { get; set; } = [];

    public VideoListRequest FilterRequest => new(DomainId, CategoryId, Status, Search, CurrentPage, 20);

    public async Task OnGetAsync()
    {
        Domains = await domainSvc.GetAllAsync(activeOnly: false);
        Categories = DomainId.HasValue
            ? await catSvc.GetByDomainAsync(DomainId.Value, activeOnly: false)
            : await catSvc.GetAllAsync(activeOnly: false);
        Videos = await svc.GetVideosAsync(FilterRequest);
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id)
    {
        try { await svc.PublishAsync(id); TempData["Success"] = "Video published."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnpublishAsync(Guid id)
    {
        try { await svc.UnpublishAsync(id); TempData["Success"] = "Video unpublished."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try { await svc.DeleteAsync(id); TempData["Success"] = "Video deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }
}
