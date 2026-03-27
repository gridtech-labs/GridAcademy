using System.Security.Claims;
using GridAcademy.DTOs.Users;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public class DeleteModel : PageModel
{
    private readonly IUserService _users;

    public DeleteModel(IUserService users) => _users = users;

    public UserDto? UserInfo { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            UserInfo = await _users.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        // Guard: cannot delete your own account
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId == id.ToString())
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToPage("./Index");
        }

        try
        {
            await _users.DeleteAsync(id);
            TempData["Success"] = "User was deleted successfully.";
        }
        catch (KeyNotFoundException)
        {
            TempData["Error"] = "User not found.";
        }

        return RedirectToPage("./Index");
    }
}
