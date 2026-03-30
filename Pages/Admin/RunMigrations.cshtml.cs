using GridAcademy.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Admin;

[Authorize(Roles = "Admin")]
public class RunMigrationsModel(AppDbContext db, ILogger<RunMigrationsModel> logger) : PageModel
{
    public string?              Result             { get; private set; }
    public bool                 Success            { get; private set; }
    public IEnumerable<string>? PendingMigrations  { get; private set; }

    public async Task OnGetAsync()
    {
        try
        {
            PendingMigrations = await db.Database.GetPendingMigrationsAsync();
        }
        catch (Exception ex)
        {
            Result  = ex.Message;
            Success = false;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count == 0)
            {
                Result  = "No pending migrations — database is already up to date.";
                Success = true;
                PendingMigrations = [];
                return Page();
            }

            logger.LogInformation("[Manual Migration] Applying {Count} pending migration(s): {Names}",
                pending.Count, string.Join(", ", pending));

            await db.Database.MigrateAsync();

            Result  = $"Successfully applied {pending.Count} migration(s):\n" +
                      string.Join("\n", pending.Select(m => $"  ✓ {m}"));
            Success = true;
            PendingMigrations = [];

            logger.LogInformation("[Manual Migration] Completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Manual Migration] Failed.");
            Result  = ex.ToString();
            Success = false;

            try { PendingMigrations = await db.Database.GetPendingMigrationsAsync(); }
            catch { /* ignore */ }
        }

        return Page();
    }
}
