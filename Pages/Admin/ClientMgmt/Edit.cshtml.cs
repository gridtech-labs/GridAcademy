using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using GridAcademy.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Admin.ClientMgmt;

[Authorize(Roles = "SuperAdmin")]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db) => _db = db;

    [BindProperty]
    public ClientEditInput Input { get; set; } = new();

    public int UserCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = await _db.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client is null) return NotFound();

        UserCount = await _db.Users.CountAsync(u => u.ClientId == id);

        Input = new ClientEditInput
        {
            Id          = client.Id,
            Name        = client.Name,
            Slug        = client.Slug,
            Description = client.Description,
            LogoUrl     = client.LogoUrl,
            IsActive    = client.IsActive
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var client = await _db.Clients.FindAsync(Input.Id);
        if (client is null) return NotFound();

        // Slug: use existing if blank, else validate uniqueness
        var slug = string.IsNullOrWhiteSpace(Input.Slug)
            ? GenerateSlug(Input.Name)
            : Input.Slug.Trim().ToLower();

        var slugTaken = await _db.Clients
            .AnyAsync(c => c.Slug == slug && c.Id != Input.Id);

        if (slugTaken)
        {
            ModelState.AddModelError(nameof(Input.Slug),
                $"Slug '{slug}' is already used by another client.");
            return Page();
        }

        client.Name        = Input.Name.Trim();
        client.Slug        = slug;
        client.Description = Input.Description?.Trim();
        client.LogoUrl     = Input.LogoUrl?.Trim();
        client.IsActive    = Input.IsActive;

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Client '{client.Name}' was updated successfully.";
        return RedirectToPage("./Index");
    }

    private static string GenerateSlug(string name)
        => Regex.Replace(name.Trim().ToLower(), @"[^a-z0-9]+", "-").Trim('-');
}

public class ClientEditInput
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Client name is required."), MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(220)]
    public string? Slug { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(500), Url(ErrorMessage = "Enter a valid URL.")]
    public string? LogoUrl { get; set; }

    public bool IsActive { get; set; } = true;
}
