using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;
using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.ContentFiles;

[Authorize(Roles = "Admin,Instructor")]
public class EditModel(IContentFileService svc, IDomainService domainSvc) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public int DomainId { get; set; }
    [BindProperty] public string Title { get; set; } = "";
    [BindProperty] public string? Description { get; set; }
    [BindProperty] public ContentFileType ContentType { get; set; }
    [BindProperty] public bool IsActive { get; set; } = true;

    public bool IsEdit => Id != Guid.Empty;
    public List<DomainDto> Domains { get; set; } = [];
    public string? FilePath { get; set; }
    public string? OriginalFileName { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Domains = await domainSvc.GetAllAsync();
        if (IsEdit)
        {
            try
            {
                var f = await svc.GetByIdAsync(Id);
                DomainId = f.DomainId; Title = f.Title; Description = f.Description;
                ContentType = f.ContentType; IsActive = f.IsActive;
                FilePath = f.FilePath; OriginalFileName = f.OriginalFileName;
            }
            catch { return NotFound(); }
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(IFormFile? ContentFile)
    {
        Domains = await domainSvc.GetAllAsync();
        if (!ModelState.IsValid) return Page();
        try
        {
            var request = new CreateContentFileRequest(DomainId, Title, Description, ContentType, IsActive);
            if (IsEdit) await svc.UpdateAsync(Id, request, ContentFile);
            else await svc.CreateAsync(request, ContentFile);
            TempData["Success"] = IsEdit ? "Content file updated." : "Content file uploaded.";
            return RedirectToPage("Index");
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; return Page(); }
    }
}
