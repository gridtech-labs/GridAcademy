using GridAcademy.Common;
using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;
using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.ContentFiles;

[Authorize(Roles = "Admin,Instructor")]
public class IndexModel(IContentFileService svc, IDomainService domainSvc) : PageModel
{
    [BindProperty(SupportsGet = true)] public ContentFileListRequest FilterRequest { get; set; } = new(null, null, null);

    public PagedResult<ContentFileDto> Files { get; set; } = new();
    public List<DomainDto> Domains { get; set; } = [];

    public async Task OnGetAsync()
    {
        Domains = await domainSvc.GetAllAsync();
        Files = await svc.GetFilesAsync(FilterRequest);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try { await svc.DeleteAsync(id); TempData["Success"] = "Content file deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }
}
