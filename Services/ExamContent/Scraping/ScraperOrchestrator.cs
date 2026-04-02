using GridAcademy.Services.ExamContent.Scraping.Models;

namespace GridAcademy.Services.ExamContent.Scraping;

public class ScraperOrchestrator(
    IEnumerable<IScraper> scrapers,
    ILogger<ScraperOrchestrator> logger)
{
    public Task<List<ScrapedNotification>> RunAsync(CancellationToken ct = default) => FetchAllAsync(ct);

    public async Task<List<ScrapedNotification>> FetchAllAsync(CancellationToken ct = default)
    {
        var allNotifications = new List<ScrapedNotification>();

        foreach (var scraper in scrapers)
        {
            try
            {
                var notifications = await ExecuteWithRetryAsync(scraper, ct);
                logger.LogInformation("Fetched {Count} notifications from {SourceKey}", notifications.Count, scraper.SourceKey);
                allNotifications.AddRange(notifications);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Scraper fetch failed for source {SourceKey}.", scraper.SourceKey);
            }
        }

        return allNotifications;
    }

    private async Task<List<ScrapedNotification>> ExecuteWithRetryAsync(IScraper scraper, CancellationToken ct)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await scraper.FetchAsync(ct);
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Fetch failed for {SourceKey} attempt {Attempt}/{MaxAttempts}. Retrying...", scraper.SourceKey, attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }
        }

        return [];
    }
}
