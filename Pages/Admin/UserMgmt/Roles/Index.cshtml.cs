using GridAcademy.DTOs.Users;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.UserMgmt.Roles;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IRoleService _roles;

    public IndexModel(IRoleService roles) => _roles = roles;

    public List<SystemRoleDto> Roles { get; set; } = [];

    [BindProperty] public CreateSystemRoleRequest Form { get; set; } = new();

    // For inline edit
    [BindProperty] public int          EditId          { get; set; }
    [BindProperty] public CreateSystemRoleRequest EditForm { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        Roles = await _roles.GetRolesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            Roles = await _roles.GetRolesAsync();
            return Page();
        }
        try
        {
            await _roles.CreateAsync(Form);
            TempData["Success"] = $"Role \"{Form.DisplayName}\" created.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        try
        {
            await _roles.UpdateAsync(EditId, EditForm);
            TempData["Success"] = "Role updated.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        try
        {
            await _roles.ToggleActiveAsync(id);
            TempData["Success"] = "Role status updated.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _roles.DeleteAsync(id);
            TempData["Success"] = "Role deleted.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage();
    }
}
