using GridAcademy.Data.Entities.Exam;
using GridAcademy.Services.ExamContent.Scraping.Models;
using GridAcademy.Services.ExamContent.Scraping.Options;
using Microsoft.Extensions.Options;

namespace GridAcademy.Services.ExamContent.Scraping.Scrapers;

public class UpscScraper(
    HttpClient httpClient,
    IOptions<ScrapingOptions> scrapingOptions,
    ILogger<UpscScraper> logger) : BaseHtmlScraper(httpClient, logger), IScraper
{
    public string SourceKey => "Upsc";

    public async Task<List<ScrapedNotification>> FetchAsync(CancellationToken ct = default)
    {
        if (!scrapingOptions.Value.Sources.TryGetValue(SourceKey, out var sourceUrl) || string.IsNullOrWhiteSpace(sourceUrl))
            return [];

        var doc = await LoadDocumentAsync(sourceUrl, ct);
        var links = doc.DocumentNode.SelectNodes("//a[@href]") ?? [];

        return links
            .Take(40)
            .Select(node => new ScrapedNotification
            {
                Title = Normalize(node.InnerText),
                SourceUrl = ToAbsoluteUrl(node.GetAttributeValue("href", sourceUrl), sourceUrl),
                ContentHtml = node.OuterHtml,
                NotificationType = ExamNotificationType.Notification
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Title) && x.Title.Length >= 10)
            .GroupBy(x => x.SourceUrl, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }
}
