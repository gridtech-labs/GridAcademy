using GridAcademy.Data;
using GridAcademy.DTOs.Users;
using GridAcademy.Jobs;
using GridAcademy.Services;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Admin.Users;

[Authorize(Roles = "Admin")]   // SuperAdmin inherits Admin via IClaimsTransformation
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IUserService _users;
    private readonly IRoleService _roles;
    private readonly IBackgroundJobClient _jobs;

    public CreateModel(AppDbContext db, IUserService users, IRoleService roles, IBackgroundJobClient jobs)
    {
        _db    = db;
        _users = users;
        _roles = roles;
        _jobs  = jobs;
    }

    [BindProperty]
    public CreateUserRequest Input { get; set; } = new();

    public List<SystemRoleDto> AvailableRoles { get; set; } = [];

    /// <summary>Clients available to assign. SuperAdmin sees all; Admin sees only their own.</summary>
    public List<ClientSelectItem> AvailableClients { get; set; } = [];

    public bool IsSuperAdmin => User.IsInRole("SuperAdmin");

    public async Task OnGetAsync()
    {
        AvailableRoles = await _roles.GetRolesAsync();
        await LoadClientsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            AvailableRoles = await _roles.GetRolesAsync();
            await LoadClientsAsync();
            return Page();
        }

        // Enforce client scoping: non-SuperAdmin can only create in their own client
        if (!IsSuperAdmin)
        {
            var cidClaim = User.FindFirst("ClientId")?.Value;
            if (int.TryParse(cidClaim, out var cid))
                Input.ClientId = cid;
        }

        try
        {
            var user = await _users.CreateAsync(Input);

            // Fire-and-forget welcome email — Hangfire retries on failure
            _jobs.Enqueue<EmailJob>(j => j.SendWelcomeEmailAsync(user.Email, user.FullName));

            TempData["Success"] = $"User '{user.FullName}' was created successfully.";
            return RedirectToPage("./Index");
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            AvailableRoles = await _roles.GetRolesAsync();
            await LoadClientsAsync();
            return Page();
        }
    }

    private async Task LoadClientsAsync()
    {
        if (IsSuperAdmin)
        {
            AvailableClients = await _db.Clients
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new ClientSelectItem(c.Id, c.Name))
                .ToListAsync();
        }
        else
        {
            // Admin: show only their own client and pre-fill
            var cidClaim = User.FindFirst("ClientId")?.Value;
            if (int.TryParse(cidClaim, out var cid))
            {
                var client = await _db.Clients.FindAsync(cid);
                if (client is not null)
                {
                    AvailableClients = [new ClientSelectItem(client.Id, client.Name)];
                    Input.ClientId = cid;
                }
            }
        }
    }
}

public record ClientSelectItem(int Id, string Name);
