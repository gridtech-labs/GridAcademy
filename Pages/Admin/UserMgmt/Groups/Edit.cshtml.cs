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
public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db) => _db = db;

    // ── Bound form for group details ─────────────────────────────────────────
    [BindProperty]
    public GroupEditInput Input { get; set; } = new();

    // ── Read-only display ────────────────────────────────────────────────────
    public GroupDetail? Group     { get; set; }
    public List<MemberRow> Members  { get; set; } = [];
    public List<UserSearchRow> SearchResults { get; set; } = [];

    // ── Query params ─────────────────────────────────────────────────────────
    [BindProperty(SupportsGet = true)]
    public string? MemberSearch { get; set; }

    public bool IsSuperAdmin => User.IsInRole("SuperAdmin");

    // ════════════════════════════════════════════════════════════════════════
    // GET — load group + members + optional user search
    // ════════════════════════════════════════════════════════════════════════
    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadGroupAsync(id)) return NotFound();
        await LoadMembersAsync(id);
        if (!string.IsNullOrWhiteSpace(MemberSearch))
            await RunUserSearchAsync(id);
        return Page();
    }

    // ════════════════════════════════════════════════════════════════════════
    // POST — save group name / description / isActive
    // ════════════════════════════════════════════════════════════════════════
    public async Task<IActionResult> OnPostSaveDetailsAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadGroupAsync(Input.Id);
            await LoadMembersAsync(Input.Id);
            return Page();
        }

        var group = await _db.Groups.FindAsync(Input.Id);
        if (group is null) return NotFound();

        group.Name        = Input.Name.Trim();
        group.Description = Input.Description?.Trim();
        group.IsActive    = Input.IsActive;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Group details saved.";
        return RedirectToPage(new { id = Input.Id });
    }

    // ════════════════════════════════════════════════════════════════════════
    // POST — add a user to this group
    // ════════════════════════════════════════════════════════════════════════
    public async Task<IActionResult> OnPostAddMemberAsync(int groupId, Guid userId)
    {
        var exists = await _db.UserGroups
            .AnyAsync(ug => ug.GroupId == groupId && ug.UserId == userId);

        if (!exists)
        {
            var addedBy = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid)
                ? uid : (Guid?)null;

            _db.UserGroups.Add(new UserGroup
            {
                GroupId = groupId,
                UserId  = userId,
                AddedBy = addedBy
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Member added.";
        }
        else
        {
            TempData["Info"] = "User is already a member of this group.";
        }

        return RedirectToPage(new { id = groupId, memberSearch = MemberSearch });
    }

    // ════════════════════════════════════════════════════════════════════════
    // POST — remove a user from this group
    // ════════════════════════════════════════════════════════════════════════
    public async Task<IActionResult> OnPostRemoveMemberAsync(int groupId, int membershipId)
    {
        var membership = await _db.UserGroups.FindAsync(membershipId);
        if (membership is not null)
        {
            _db.UserGroups.Remove(membership);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Member removed.";
        }

        return RedirectToPage(new { id = groupId });
    }

    // ════════════════════════════════════════════════════════════════════════
    // Private helpers
    // ════════════════════════════════════════════════════════════════════════
    private async Task<bool> LoadGroupAsync(int id)
    {
        var g = await _db.Groups
            .AsNoTracking()
            .Include(x => x.Client)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (g is null) return false;

        Group = new GroupDetail(g.Id, g.Name, g.Description, g.IsActive,
                                g.Client?.Name, g.ClientId, g.CreatedAt);

        Input = new GroupEditInput
        {
            Id          = g.Id,
            Name        = g.Name,
            Description = g.Description,
            IsActive    = g.IsActive
        };
        return true;
    }

    private async Task LoadMembersAsync(int groupId)
    {
        Members = await _db.UserGroups
            .AsNoTracking()
            .Where(ug => ug.GroupId == groupId)
            .Include(ug => ug.User)
            .OrderBy(ug => ug.User.FirstName).ThenBy(ug => ug.User.LastName)
            .Select(ug => new MemberRow
            {
                MembershipId = ug.Id,
                UserId       = ug.UserId,
                FullName     = ug.User.FirstName + " " + ug.User.LastName,
                Email        = ug.User.Email,
                Role         = ug.User.Role,
                AddedAt      = ug.AddedAt
            })
            .ToListAsync();
    }

    private async Task RunUserSearchAsync(int groupId)
    {
        var term          = MemberSearch!.Trim().ToLower();
        var memberUserIds = Members.Select(m => m.UserId).ToHashSet();

        // Always scope candidates to the group's own client.
        // For a client-specific group every user added must belong to that same client,
        // regardless of whether the current admin is SuperAdmin or a regular Admin.
        // A group with no ClientId (global/platform group) is SuperAdmin-only and shows all users.
        var query = _db.Users.AsNoTracking().AsQueryable();
        var groupClientId = Group?.ClientId;
        if (groupClientId.HasValue)
            query = query.Where(u => u.ClientId == groupClientId.Value);
        else if (!IsSuperAdmin)
            query = query.Where(_ => false); // non-SuperAdmin should never reach a client-less group

        SearchResults = await query
            .Where(u => u.IsActive &&
                        (u.FirstName.ToLower().Contains(term) ||
                         u.LastName.ToLower().Contains(term)  ||
                         u.Email.ToLower().Contains(term)))
            .OrderBy(u => u.FirstName)
            .Take(20)
            .Select(u => new UserSearchRow
            {
                Id       = u.Id,
                FullName = u.FirstName + " " + u.LastName,
                Email    = u.Email,
                Role     = u.Role,
                AlreadyMember = memberUserIds.Contains(u.Id)
            })
            .ToListAsync();
    }
}

// ── View models ───────────────────────────────────────────────────────────────

public record GroupDetail(int Id, string Name, string? Description, bool IsActive,
                          string? ClientName, int? ClientId, DateTime CreatedAt);

public class GroupEditInput
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Group name is required."), MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class MemberRow
{
    public int      MembershipId { get; set; }
    public Guid     UserId       { get; set; }
    public string   FullName     { get; set; } = "";
    public string   Email        { get; set; } = "";
    public string   Role         { get; set; } = "";
    public DateTime AddedAt      { get; set; }
}

public class UserSearchRow
{
    public Guid   Id            { get; set; }
    public string FullName      { get; set; } = "";
    public string Email         { get; set; } = "";
    public string Role          { get; set; } = "";
    public bool   AlreadyMember { get; set; }
}
