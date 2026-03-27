using GridAcademy.DTOs.VideoLearning;
using GridAcademy.Services.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.VideoLearning.Channels;

[Authorize(Roles = "Admin")]
public class IndexModel(ISalesChannelService svc) : PageModel
{
    public List<SalesChannelDto> Channels { get; set; } = [];

    public async Task OnGetAsync()
    {
        Channels = await svc.GetAllAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try { await svc.DeleteAsync(id); TempData["Success"] = "Channel deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }
}
