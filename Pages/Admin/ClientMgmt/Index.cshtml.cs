using GridAcademy.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Admin.ClientMgmt;

[Authorize(Roles = "SuperAdmin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public List<ClientRow> Clients { get; set; } = [];

    public async Task OnGetAsync()
    {
        Clients = await _db.Clients
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new ClientRow
            {
                Id        = c.Id,
                Name      = c.Name,
                Slug      = c.Slug,
                IsActive  = c.IsActive,
                UserCount = c.Users.Count,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();
    }

    public class ClientRow
    {
        public int      Id        { get; set; }
        public string   Name      { get; set; } = "";
        public string   Slug      { get; set; } = "";
        public bool     IsActive  { get; set; }
        public int      UserCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
