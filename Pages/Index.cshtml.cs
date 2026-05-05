using GridAcademy.DTOs.Marketplace;
using GridAcademy.Services.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly IStorefrontService _storefront;

    public IndexModel(IStorefrontService storefront)
    {
        _storefront = storefront;
    }

    public HomepageDto? Data { get; set; }

    public async Task OnGetAsync()
    {
        Data = await _storefront.GetHomepageAsync();
    }
}
