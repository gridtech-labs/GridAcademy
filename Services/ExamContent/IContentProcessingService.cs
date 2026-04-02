using GridAcademy.Services.ExamContent.Models;
using GridAcademy.Services.ExamContent.Scraping.Models;

namespace GridAcademy.Services.ExamContent;

public interface IContentProcessingService
{
    string CleanHtml(string htmlContent);
    string ExtractSummary(string cleanedText, int wordLimit = 180);
    string GenerateSlug(string input);
    string GenerateContentHash(string content);

    Task<ContentProcessingResult> ProcessAsync(ScrapedNotification notification, CancellationToken ct = default);
}
