using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using GridAcademy.Data;
using GridAcademy.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Admin.ClientMgmt;

[Authorize(Roles = "SuperAdmin")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;

    public CreateModel(AppDbContext db) => _db = db;

    [BindProperty]
    public ClientInput Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // Auto-generate slug from name if blank
        var slug = string.IsNullOrWhiteSpace(Input.Slug)
            ? GenerateSlug(Input.Name)
            : Input.Slug.Trim().ToLower();

        // Check slug uniqueness
        if (await _db.Clients.AnyAsync(c => c.Slug == slug))
        {
            ModelState.AddModelError(nameof(Input.Slug),
                $"Slug '{slug}' is already taken. Choose a different one.");
            return Page();
        }

        _db.Clients.Add(new Client
        {
            Name        = Input.Name.Trim(),
            Slug        = slug,
            Description = Input.Description?.Trim(),
            LogoUrl     = Input.LogoUrl?.Trim(),
            IsActive    = Input.IsActive
        });

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Client '{Input.Name}' was created successfully.";
        return RedirectToPage("./Index");
    }

    private static string GenerateSlug(string name)
        => Regex.Replace(name.Trim().ToLower(), @"[^a-z0-9]+", "-").Trim('-');
}

public class ClientInput
{
    [Required(ErrorMessage = "Client name is required."), MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Leave blank to auto-generate from Name.</summary>
    [MaxLength(220)]
    public string? Slug { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(500), Url(ErrorMessage = "Enter a valid URL.")]
    public string? LogoUrl { get; set; }

    public bool IsActive { get; set; } = true;
}
