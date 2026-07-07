using GridAcademy.Data;
using GridAcademy.Modules.AiGeneration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GridAcademy.Modules.AiGeneration.Jobs;

/// <summary>
/// Recurring Hangfire job (every 15 minutes) that resets any GenerationJob
/// stuck in Running state for more than 2 hours.
///
/// Why jobs get stuck: Railway restarts the app on every deploy, killing the
/// Hangfire worker mid-execution. generation_jobs.status stays "Running"
/// because RunJobAsync never reaches the finally block that saves the status.
/// </summary>
public sealed class OrphanedJobCleanerJob
{
    private readonly AppDbContext _db;
    private readonly ILogger<OrphanedJobCleanerJob> _log;

    public OrphanedJobCleanerJob(AppDbContext db, ILogger<OrphanedJobCleanerJob> log)
    {
        _db  = db;
        _log = log;
    }

    public async Task RunAsync()
    {
        var cutoff = DateTime.UtcNow.AddHours(-2);

        List<GenerationJob> orphaned;
        try
        {
            orphaned = await _db.GenerationJobs
                .Where(j => j.Status == GenerationJobStatus.Running && j.CreatedAt < cutoff)
                .ToListAsync();
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState is "42P01" or "42703")
        {
            // AI tables/columns not created yet (first deploy — the background seeder
            // runs shortly after startup). Skip this run; the next tick will succeed.
            _log.LogInformation("OrphanedJobCleaner: schema not ready yet ({State}); skipping.", ex.SqlState);
            return;
        }

        if (orphaned.Count == 0) return;

        foreach (var j in orphaned)
        {
            j.Status       = GenerationJobStatus.Failed;
            j.ErrorMessage = $"Job automatically reset after running for more than 2 hours " +
                             $"(started {j.CreatedAt:dd MMM HH:mm} UTC). " +
                             "The server was likely restarted mid-execution. " +
                             "Any drafts generated before the restart are in the Review queue. " +
                             "Re-queue this job to generate the remainder.";
            j.CompletedAt  = j.CompletedAt ?? DateTime.UtcNow;

            _log.LogWarning(
                "OrphanedJobCleaner: reset job #{JobId} (started {StartedAt:u}) to Failed.",
                j.Id, j.CreatedAt);
        }

        await _db.SaveChangesAsync();
        _log.LogInformation("OrphanedJobCleaner: reset {Count} orphaned job(s).", orphaned.Count);
    }
}
