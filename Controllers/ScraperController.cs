using GridAcademy.Services.ExamContent;
using GridAcademy.Services.ExamContent.Models;
using GridAcademy.Services.ExamContent.Scraping;
using Microsoft.AspNetCore.Mvc;

namespace GridAcademy.Controllers;

[ApiController]
[Route("api/scraper")]
public class ScraperController(
    ScraperOrchestrator orchestrator,
    IContentProcessingService contentProcessingService,
    ILogger<ScraperController> logger) : ControllerBase
{
    [HttpPost("run")]
    public async Task<IActionResult> Run(CancellationToken ct)
    {
        var items = await orchestrator.FetchAllAsync(ct);

        var saved = 0;
        var duplicates = 0;
        var errors = 0;

        foreach (var item in items)
        {
            try
            {
                logger.LogInformation("Processing scraped item {Title} ({SourceUrl})", item.Title, item.SourceUrl);
                var result = await contentProcessingService.ProcessAsync(item, ct);

                switch (result.Status)
                {
                    case ContentProcessingStatus.Saved:
                        saved++;
                        logger.LogInformation("Saved scraped item as draft with slug {Slug}", result.Slug);
                        break;
                    case ContentProcessingStatus.SkippedDuplicate:
                        duplicates++;
                        logger.LogInformation("Duplicate skipped: {Message}", result.Message);
                        break;
                    case ContentProcessingStatus.Error:
                        errors++;
                        logger.LogError("Processing failed: {Message}", result.Message);
                        break;
                }
            }
            catch (Exception ex)
            {
                errors++;
                logger.LogError(ex, "Unhandled item error for source {SourceUrl}", item.SourceUrl);
            }
        }

        return Ok(new
        {
            totalFetched = items.Count,
            saved,
            duplicates,
            errors
        });
    }
}
