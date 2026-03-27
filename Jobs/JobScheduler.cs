using Hangfire;

namespace GridAcademy.Jobs;

/// <summary>
/// Central place to register all recurring Hangfire jobs.
/// Called once from Program.cs after app is built.
///
/// Cron reference:
///   "0 2 * * *"  → every day at 02:00 AM UTC
///   "0 7 * * 1"  → every Monday at 07:00 AM UTC
///   "*/5 * * * *" → every 5 minutes (useful for testing)
/// </summary>
public static class JobScheduler
{
    public static void RegisterAll()
    {
        // Check for inactive users — daily at 02:00 AM UTC
        RecurringJob.AddOrUpdate<InactiveUserJob>(
            recurringJobId: "inactive-user-check",
            methodCall:     job => job.RunAsync(),
            cronExpression: "0 2 * * *",
            options: new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // Add more recurring jobs here as the product grows:
        // RecurringJob.AddOrUpdate<ReportJob>("weekly-report", j => j.RunAsync(), "0 7 * * 1");
    }
}
