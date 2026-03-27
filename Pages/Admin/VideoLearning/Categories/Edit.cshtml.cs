using GridAcademy.DTOs.VideoLearning;
using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.Categories;

[Authorize(Roles = "Admin,Instructor")]
public class EditModel(IVideoCategoryService svc, IDomainService domainSvc) : PageModel
{
    [BindProperty(SupportsGet = true)] public int Id { get; set; }
    [BindProperty] public int DomainId { get; set; }
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public int SortOrder { get; set; }
    [BindProperty] public bool IsActive { get; set; } = true;

    public bool IsEdit => Id > 0;
    public List<DomainDto> Domains { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        Domains = await domainSvc.GetAllAsync();
        if (IsEdit)
        {
            try
            {
                var c = await svc.GetByIdAsync(Id);
                DomainId = c.DomainId; Name = c.Name; SortOrder = c.SortOrder; IsActive = c.IsActive;
            }
            catch { return NotFound(); }
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Domains = await domainSvc.GetAllAsync();
        if (!ModelState.IsValid) return Page();
        try
        {
            var req = new CreateVideoCategoryRequest(DomainId, Name, SortOrder, IsActive);
            if (IsEdit) await svc.UpdateAsync(Id, req);
            else await svc.CreateAsync(req);
            TempData["Success"] = IsEdit ? "Category updated." : "Category created.";
            return RedirectToPage("Index");
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; return Page(); }
    }
}
