using GridAcademy.DTOs.VideoLearning;
using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.LearningPaths;

[Authorize(Roles = "Admin,Instructor")]
public class EditModel(ILearningPathService svc, IDomainService domainSvc) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }

    [BindProperty] public int DomainId { get; set; }
    [BindProperty] public string Title { get; set; } = "";
    [BindProperty] public string? Description { get; set; }
    [BindProperty] public int SortOrder { get; set; }
    [BindProperty] public bool IsActive { get; set; } = true;

    public bool IsEdit => Id.HasValue && Id != Guid.Empty;
    public string? ThumbnailPath { get; set; }
    public List<DomainDto> Domains { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        Domains = await domainSvc.GetAllAsync(activeOnly: false);
        if (IsEdit)
        {
            try
            {
                var lp = await svc.GetByIdAsync(Id!.Value);
                DomainId = lp.DomainId; Title = lp.Title; Description = lp.Description;
                SortOrder = lp.SortOrder; IsActive = lp.IsActive; ThumbnailPath = lp.ThumbnailPath;
            }
            catch { return NotFound(); }
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(IFormFile? ThumbnailFile)
    {
        Domains = await domainSvc.GetAllAsync(activeOnly: false);
        if (!ModelState.IsValid) return Page();

        try
        {
            if (IsEdit)
            {
                var req = new UpdateLearningPathRequest(Title, Description, IsActive, SortOrder);
                await svc.UpdateAsync(Id!.Value, req, ThumbnailFile);
                TempData["Success"] = "Learning path updated.";
                return RedirectToPage("Builder", new { id = Id });
            }
            else
            {
                var req = new CreateLearningPathRequest(DomainId, Title, Description, IsActive, SortOrder);
                var created = await svc.CreateAsync(req, ThumbnailFile);
                TempData["Success"] = "Learning path created. Now add content below.";
                return RedirectToPage("Builder", new { id = created.Id });
            }
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; return Page(); }
    }
}
