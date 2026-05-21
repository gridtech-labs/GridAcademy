using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages;

/// <summary>
/// Root path "/" — redirect to /Admin (dashboard if authenticated,
/// otherwise the cookie middleware forwards to /Account/Login).
/// </summary>
[AllowAnonymous]
public class IndexModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Admin/Index");
}
