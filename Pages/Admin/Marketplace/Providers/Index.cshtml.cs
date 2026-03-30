using GridAcademy.Data;
using GridAcademy.Data.Entities.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Admin.Marketplace.Providers;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public List<ProviderRow> Providers { get; set; } = [];
    [TempData] public string? SuccessMsg { get; set; }

    public async Task OnGetAsync()
    {
        Providers = await _db.MpProviders
            .Include(p => p.User)
            .Include(p => p.TestSeries)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProviderRow
            {
                Id            = p.Id,
                InstituteName = p.InstituteName,
                UserEmail     = p.User.Email,
                Status        = p.Status,
                SeriesCount   = p.TestSeries.Count,
                City          = p.City,
                CreatedAt     = p.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var p = await _db.MpProviders.FindAsync(id);
        if (p != null) { p.Status = ProviderStatus.Verified; p.UpdatedAt = DateTime.UtcNow; await _db.SaveChangesAsync(); SuccessMsg = "Provider approved."; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSuspendAsync(int id)
    {
        var p = await _db.MpProviders.FindAsync(id);
        if (p != null) { p.Status = ProviderStatus.Suspended; p.UpdatedAt = DateTime.UtcNow; await _db.SaveChangesAsync(); SuccessMsg = "Provider suspended."; }
        return RedirectToPage();
    }

    public class ProviderRow
    {
        public int            Id            { get; set; }
        public string         InstituteName { get; set; } = "";
        public string         UserEmail     { get; set; } = "";
        public ProviderStatus Status        { get; set; }
        public int            SeriesCount   { get; set; }
        public string?        City          { get; set; }
        public DateTime       CreatedAt     { get; set; }
    }
}
