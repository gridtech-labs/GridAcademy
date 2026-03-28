using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GridAcademy.DTOs.Marketplace;
using GridAcademy.Services.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Provider;

[Authorize(Roles = "Provider")]
public class ProfileModel : PageModel
{
    private readonly IProviderService _providers;

    public ProfileModel(IProviderService providers)
    {
        _providers = providers;
    }

    [BindProperty]
    public ProfileForm Form { get; set; } = new();

    public ProviderDto? Provider { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            Provider = await _providers.GetProfileAsync(userId, ct);
            Form = new ProfileForm
            {
                InstituteName = Provider.InstituteName,
                City          = Provider.City,
                State         = Provider.State,
                Bio           = Provider.Bio,
                LogoUrl       = Provider.LogoUrl
            };
        }
        catch (KeyNotFoundException)
        {
            TempData["Error"] = "Provider profile not found. Please contact support.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return Page();

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            Provider = await _providers.UpdateProfileAsync(userId, new UpdateProviderProfileRequest(
                Form.InstituteName,
                Form.City,
                Form.State,
                Form.Bio,
                Form.LogoUrl
            ), ct);

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToPage();
        }
        catch (KeyNotFoundException ex)
        {
            TempData["Error"] = ex.Message;
            return Page();
        }
    }

    public class ProfileForm
    {
        [Required(ErrorMessage = "Institute name is required.")]
        [MaxLength(200)]
        [Display(Name = "Institute Name")]
        public string InstituteName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        [MaxLength(500)]
        public string? Bio { get; set; }

        [MaxLength(300)]
        [Display(Name = "Logo URL")]
        public string? LogoUrl { get; set; }
    }
}
