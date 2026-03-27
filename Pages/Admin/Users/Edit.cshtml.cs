using System.ComponentModel.DataAnnotations;
using GridAcademy.Data;
using GridAcademy.DTOs.Users;
using GridAcademy.Helpers;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IUserService _users;

    public EditModel(AppDbContext db, IUserService users)
    {
        _db    = db;
        _users = users;
    }

    [BindProperty]
    public EditUserInput Input { get; set; } = new();

    // Displayed read-only on the form
    public string Email    { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        Email    = user.Email;
        FullName = user.FullName;

        Input = new EditUserInput
        {
            Id        = user.Id,
            FirstName = user.FirstName,
            LastName  = user.LastName,
            Role      = user.Role,
            IsActive  = user.IsActive
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Re-load email for display if we return Page()
        var existing = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == Input.Id);
        if (existing is null) return NotFound();
        Email    = existing.Email;
        FullName = existing.FullName;

        if (!ModelState.IsValid) return Page();

        try
        {
            // Optional password change — if field is blank, skip it
            if (!string.IsNullOrWhiteSpace(Input.NewPassword))
            {
                var pwError = PasswordHelper.Validate(Input.NewPassword);
                if (pwError is not null)
                {
                    ModelState.AddModelError(nameof(Input.NewPassword), pwError);
                    return Page();
                }

                var userEntity = await _db.Users.FindAsync(Input.Id);
                if (userEntity is not null)
                {
                    userEntity.PasswordHash = PasswordHelper.Hash(Input.NewPassword);
                    await _db.SaveChangesAsync();
                }
            }

            await _users.UpdateAsync(Input.Id, new UpdateUserRequest
            {
                FirstName = Input.FirstName,
                LastName  = Input.LastName,
                Role      = Input.Role,
                IsActive  = Input.IsActive
            });

            TempData["Success"] = $"User '{Input.FirstName} {Input.LastName}' was updated successfully.";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}

public class EditUserInput
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "User";

    public bool IsActive { get; set; } = true;

    /// <summary>Leave blank to keep the existing password.</summary>
    public string? NewPassword { get; set; }
}
