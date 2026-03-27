using GridAcademy.DTOs.VideoLearning;
using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.Channels;

[Authorize(Roles = "Admin")]
public class EditModel(ISalesChannelService svc) : PageModel
{
    [BindProperty(SupportsGet = true)] public int Id { get; set; }

    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string? BaseUrl { get; set; }
    [BindProperty] public bool IsActive { get; set; } = true;

    public bool IsEdit => Id > 0;
    public int ChannelId => Id;

    public async Task<IActionResult> OnGetAsync()
    {
        if (IsEdit)
        {
            try
            {
                var ch = await svc.GetByIdAsync(Id);
                Name = ch.Name; BaseUrl = ch.BaseUrl; IsActive = ch.IsActive;
            }
            catch { return NotFound(); }
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        try
        {
            if (IsEdit)
            {
                await svc.UpdateAsync(Id, new UpdateSalesChannelRequest(Name, BaseUrl, IsActive));
                TempData["Success"] = "Channel updated.";
            }
            else
            {
                var result = await svc.CreateAsync(new CreateSalesChannelRequest(Name, BaseUrl, IsActive));
                TempData["NewApiKey"] = result.RawApiKey;
                TempData["Success"] = "Channel created.";
            }
            return RedirectToPage("Index");
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; return Page(); }
    }
}
