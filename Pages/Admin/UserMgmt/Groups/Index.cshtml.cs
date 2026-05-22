using System.Security.Claims;
using GridAcademy.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Admin.UserMgmt.Groups;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public List<GroupRow> Groups { get; set; } = [];
    public bool IsSuperAdmin => User.IsInRole("SuperAdmin");

    public async Task OnGetAsync()
    {
        var query = _db.Groups.AsNoTracking().Include(g => g.Client).AsQueryable();

        // Scope to current user's client unless SuperAdmin
        if (!IsSuperAdmin)
        {
            var cidClaim = User.FindFirst("ClientId")?.Value;
            if (int.TryParse(cidClaim, out var cid))
                query = query.Where(g => g.ClientId == cid);
            else
                query = query.Where(_ => false); // no client → see nothing
        }

        Groups = await query
            .OrderByDescending(g => g.IsActive)
            .ThenBy(g => g.Name)
            .Select(g => new GroupRow
            {
                Id          = g.Id,
                Name        = g.Name,
                Description = g.Description,
                ClientName  = g.Client != null ? g.Client.Name : null,
                MemberCount = g.UserGroups.Count,
                IsActive    = g.IsActive,
                CreatedAt   = g.CreatedAt
            })
            .ToListAsync();
    }

    public class GroupRow
    {
        public int      Id          { get; set; }
        public string   Name        { get; set; } = "";
        public string?  Description { get; set; }
        public string?  ClientName  { get; set; }
        public int      MemberCount { get; set; }
        public bool     IsActive    { get; set; }
        public DateTime CreatedAt   { get; set; }
    }
}
