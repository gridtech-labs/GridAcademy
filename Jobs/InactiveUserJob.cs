using GridAcademy.Data;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Jobs;

/// <summary>
/// Recurring scheduled job: finds and logs users who have not
/// logged in for 90+ days. Can be extended to deactivate or notify them.
///
/// Registered as a recurring cron job in JobScheduler.
/// </summary>
public class InactiveUserJob
{
    private readonly AppDbContext _db;
    private readonly ILogger<InactiveUserJob> _logger;

    public InactiveUserJob(AppDbContext db, ILogger<InactiveUserJob> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-90);

        var inactiveUsers = await _db.Users
            .AsNoTracking()
            .Where(u =>
                u.IsActive &&
                (u.LastLoginAt == null || u.LastLoginAt < cutoff))
            .Select(u => new { u.Id, u.Email, u.LastLoginAt })
            .ToListAsync();

        if (inactiveUsers.Count == 0)
        {
            _logger.LogInformation("[InactiveUserJob] No inactive users found.");
            return;
        }

        _logger.LogWarning(
            "[InactiveUserJob] Found {Count} users inactive for 90+ days.", inactiveUsers.Count);

        foreach (var user in inactiveUsers)
        {
            _logger.LogInformation(
                "[InactiveUserJob] Inactive: {Email} | LastLogin: {LastLogin}",
                user.Email, user.LastLoginAt?.ToString("yyyy-MM-dd") ?? "Never");

            // Extension point: deactivate, send warning email, etc.
            // Example: await _emailJob.SendNotificationAsync(user.Email, "Account Inactive", "...");
        }
    }
}
