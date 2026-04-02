using GridAcademy.Services.ExamContent.Scraping;

namespace GridAcademy.Jobs;

public class ExamScrapingJob(ScraperOrchestrator orchestrator, ILogger<ExamScrapingJob> logger)
{
    public async Task RunAsync()
    {
        logger.LogInformation("[ExamScrapingJob] Starting scraper orchestrator run.");
        await orchestrator.RunAsync();
        logger.LogInformation("[ExamScrapingJob] Scraper orchestrator run completed.");
    }
}
