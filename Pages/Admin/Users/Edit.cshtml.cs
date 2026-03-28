using System.ComponentModel.DataAnnotations;
using GridAcademy.Data;
using GridAcademy.Data.Entities.Marketplace;
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

    public string Email    { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    // Populated when the user has a Provider profile
    public ProviderProfileInfo? ProviderProfile { get; set; }

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

        await LoadProviderProfileAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var existing = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == Input.Id);
        if (existing is null) return NotFound();
        Email    = existing.Email;
        FullName = existing.FullName;

        if (!ModelState.IsValid)
        {
            await LoadProviderProfileAsync(Input.Id);
            return Page();
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(Input.NewPassword))
            {
                var pwError = PasswordHelper.Validate(Input.NewPassword);
                if (pwError is not null)
                {
                    ModelState.AddModelError(nameof(Input.NewPassword), pwError);
                    await LoadProviderProfileAsync(Input.Id);
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
            await LoadProviderProfileAsync(Input.Id);
            return Page();
        }
    }

    // ── Provider approval handlers ────────────────────────────────────────────

    public async Task<IActionResult> OnPostApproveProviderAsync(Guid userId)
    {
        var provider = await _db.MpProviders.FirstOrDefaultAsync(p => p.UserId == userId);
        if (provider is null)
        {
            TempData["Error"] = "Provider profile not found.";
            return RedirectToPage(new { id = userId });
        }

        provider.Status    = ProviderStatus.Verified;
        provider.UpdatedAt = DateTime.UtcNow;

        // Make sure their user account is active
        var user = await _db.Users.FindAsync(userId);
        if (user is not null) user.IsActive = true;

        await _db.SaveChangesAsync();

        TempData["Success"] = $"{provider.InstituteName} has been approved as a Provider.";
        return RedirectToPage(new { id = userId });
    }

    public async Task<IActionResult> OnPostRejectProviderAsync(Guid userId)
    {
        var provider = await _db.MpProviders.FirstOrDefaultAsync(p => p.UserId == userId);
        if (provider is null)
        {
            TempData["Error"] = "Provider profile not found.";
            return RedirectToPage(new { id = userId });
        }

        provider.Status    = ProviderStatus.Suspended;
        provider.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"{provider.InstituteName} has been suspended/rejected.";
        return RedirectToPage(new { id = userId });
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private async Task LoadProviderProfileAsync(Guid userId)
    {
        var p = await _db.MpProviders.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (p is not null)
        {
            ProviderProfile = new ProviderProfileInfo(
                p.InstituteName, p.City, p.State, p.Bio,
                p.Status.ToString(), p.AgreedToTerms, p.CreatedAt);
        }
    }
}

public record ProviderProfileInfo(
    string   InstituteName,
    string?  City,
    string?  State,
    string?  Bio,
    string   Status,
    bool     AgreedToTerms,
    DateTime CreatedAt
);

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
