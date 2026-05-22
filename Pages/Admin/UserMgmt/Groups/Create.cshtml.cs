using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GridAcademy.Data;
using GridAcademy.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Admin.UserMgmt.Groups;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;

    public CreateModel(AppDbContext db) => _db = db;

    [BindProperty]
    public GroupInput Input { get; set; } = new();

    /// <summary>Clients list — SuperAdmin can pick any; Admin sees only their own.</summary>
    public List<ClientItem> AvailableClients { get; set; } = [];

    public bool IsSuperAdmin => User.IsInRole("SuperAdmin");

    public async Task OnGetAsync()
    {
        await LoadClientsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadClientsAsync();
            return Page();
        }

        // Enforce client scoping
        int? clientId = Input.ClientId;
        if (!IsSuperAdmin)
        {
            var cidClaim = User.FindFirst("ClientId")?.Value;
            clientId = int.TryParse(cidClaim, out var cid) ? cid : null;
        }

        var creatorId = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid)
            ? uid : (Guid?)null;

        _db.Groups.Add(new Group
        {
            Name        = Input.Name.Trim(),
            Description = Input.Description?.Trim(),
            ClientId    = clientId,
            IsActive    = Input.IsActive,
            CreatedBy   = creatorId
        });

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Group '{Input.Name}' created. You can now add members.";
        return RedirectToPage("./Index");
    }

    private async Task LoadClientsAsync()
    {
        if (IsSuperAdmin)
        {
            AvailableClients = await _db.Clients
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new ClientItem(c.Id, c.Name))
                .ToListAsync();
        }
        else
        {
            var cidClaim = User.FindFirst("ClientId")?.Value;
            if (int.TryParse(cidClaim, out var cid))
            {
                var client = await _db.Clients.FindAsync(cid);
                if (client is not null)
                {
                    AvailableClients = [new ClientItem(client.Id, client.Name)];
                    Input.ClientId   = cid;
                }
            }
        }
    }
}

public class GroupInput
{
    [Required(ErrorMessage = "Group name is required."), MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int? ClientId { get; set; }

    public bool IsActive { get; set; } = true;
}

public record ClientItem(int Id, string Name);
