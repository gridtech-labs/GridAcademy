using GridAcademy.DTOs.VideoLearning;
using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.Domains;

[Authorize(Roles = "Admin,Instructor")]
public class EditModel(IDomainService svc, IWebHostEnvironment env) : PageModel
{
    [BindProperty(SupportsGet = true)] public int Id { get; set; }

    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string? Description { get; set; }
    [BindProperty] public int SortOrder { get; set; }
    [BindProperty] public bool IsActive { get; set; } = true;

    public bool IsEdit => Id > 0;
    public string? LogoUrl { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (IsEdit)
        {
            try
            {
                var d = await svc.GetByIdAsync(Id);
                Name = d.Name; Description = d.Description;
                SortOrder = d.SortOrder; IsActive = d.IsActive; LogoUrl = d.LogoUrl;
            }
            catch { return NotFound(); }
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(IFormFile? LogoFile)
    {
        if (!ModelState.IsValid) return Page();

        // handle logo upload
        string? logoUrl = LogoUrl;
        if (LogoFile != null && LogoFile.Length > 0)
        {
            var dir = Path.Combine(env.WebRootPath, "uploads", "domains");
            Directory.CreateDirectory(dir);
            var ext = Path.GetExtension(LogoFile.FileName).ToLowerInvariant();
            var fileName = $"domain_{(IsEdit ? Id.ToString() : Guid.NewGuid().ToString())}{ext}";
            await using var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
            await LogoFile.CopyToAsync(stream);
            logoUrl = $"/uploads/domains/{fileName}";
        }

        try
        {
            var request = new CreateDomainRequest(Name, Description, SortOrder, IsActive);
            if (IsEdit)
            {
                await svc.UpdateAsync(Id, request);
                TempData["Success"] = "Domain updated.";
            }
            else
            {
                await svc.CreateAsync(request);
                TempData["Success"] = "Domain created.";
            }
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return Page();
        }
    }
}
