using GridAcademy.Data.Entities.Exam;
using GridAcademy.Services.ExamContent.Scraping.Models;
using GridAcademy.Services.ExamContent.Scraping.Options;
using Microsoft.Extensions.Options;

namespace GridAcademy.Services.ExamContent.Scraping.Scrapers;

public class RrbScraper(
    HttpClient httpClient,
    IOptions<ScrapingOptions> scrapingOptions,
    ILogger<RrbScraper> logger) : BaseHtmlScraper(httpClient, logger), IScraper
{
    public string SourceKey => "Rrb";

    public async Task<List<ScrapedNotification>> FetchAsync(CancellationToken ct = default)
    {
        if (!scrapingOptions.Value.Sources.TryGetValue(SourceKey, out var sourceUrl) || string.IsNullOrWhiteSpace(sourceUrl))
            return [];

        var doc = await LoadDocumentAsync(sourceUrl, ct);
        var links = doc.DocumentNode.SelectNodes("//a[@href]")?.AsEnumerable() ?? [];

        return links
            .Take(50)
            .Select(node => new ScrapedNotification
            {
                Title = Normalize(node.InnerText),
                SourceUrl = ToAbsoluteUrl(node.GetAttributeValue("href", sourceUrl), sourceUrl),
                ContentHtml = node.OuterHtml,
                NotificationType = InferType(Normalize(node.InnerText))
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Title) && x.Title.Length >= 10)
            .GroupBy(x => x.SourceUrl, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private static ExamNotificationType InferType(string title) =>
        title.Contains("result", StringComparison.OrdinalIgnoreCase)
            ? ExamNotificationType.Result
            : ExamNotificationType.Notification;
}
