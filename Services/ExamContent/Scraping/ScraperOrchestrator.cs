using GridAcademy.Repositories.ExamContent;

namespace GridAcademy.Services.ExamContent.Scraping;

public class ScraperOrchestrator(
    IEnumerable<IScraper> scrapers,
    IExamContentRepository repository,
    IContentProcessingService contentProcessingService,
    ILogger<ScraperOrchestrator> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        foreach (var scraper in scrapers)
        {
            try
            {
                var notifications = await ExecuteWithRetryAsync(scraper, ct);
                foreach (var notification in notifications)
                {
                    var cleanHtml = contentProcessingService.CleanHtml(notification.ContentHtml);
                    var hash = contentProcessingService.GenerateContentHash(cleanHtml);
                    if (await repository.HashExistsAsync(hash, ct))
                    {
                        logger.LogDebug("Duplicate notification skipped from {SourceKey}: {Url}", scraper.SourceKey, notification.SourceUrl);
                        continue;
                    }

                    notification.ContentHtml = cleanHtml;
                    await contentProcessingService.ProcessAsync(notification, hash, ct);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Scraper pipeline failed for source {SourceKey}.", scraper.SourceKey);
            }
        }
    }

    private async Task<List<Scraping.Models.ScrapedNotification>> ExecuteWithRetryAsync(IScraper scraper, CancellationToken ct)
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
