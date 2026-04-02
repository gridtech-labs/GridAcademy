using GridAcademy.Services.ExamContent.Scraping.Models;

namespace GridAcademy.Services.ExamContent;

public interface IContentProcessingService
{
    string CleanHtml(string htmlContent);
    string ExtractSummary(string htmlContent, int wordLimit = 150);
    string GenerateSlug(string input);
    string GenerateContentHash(string htmlContent);
    string NormalizeForHash(string htmlContent);
    Task ProcessAsync(ScrapedNotification notification, string? preComputedHash = null, CancellationToken ct = default);
}
