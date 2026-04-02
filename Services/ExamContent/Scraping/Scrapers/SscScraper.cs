using System.Text.RegularExpressions;
using GridAcademy.Data.Entities.Exam;
using GridAcademy.Services.ExamContent.Scraping.Models;
using GridAcademy.Services.ExamContent.Scraping.Options;
using Microsoft.Extensions.Options;

namespace GridAcademy.Services.ExamContent.Scraping.Scrapers;

public partial class SscScraper(
    HttpClient httpClient,
    IOptions<ScrapingOptions> scrapingOptions,
    ILogger<SscScraper> logger) : BaseHtmlScraper(httpClient, logger), IScraper
{
    public string SourceKey => "Ssc";

    public async Task<List<ScrapedNotification>> FetchAsync(CancellationToken ct = default)
    {
        if (!scrapingOptions.Value.Sources.TryGetValue(SourceKey, out var sourceUrl) || string.IsNullOrWhiteSpace(sourceUrl))
        {
            logger.LogWarning("No source URL configured for {SourceKey} scraper.", SourceKey);
            return [];
        }

        var doc = await LoadDocumentAsync(sourceUrl, ct);
        var linkNodes = doc.DocumentNode.SelectNodes("//a[@href]")?.AsEnumerable() ?? [];
        var items = new List<ScrapedNotification>();

        foreach (var node in linkNodes.Take(60))
        {
            var title = Normalize(node.InnerText);
            if (title.Length < 12) continue;
            if (!SscKeywordRegex().IsMatch(title)) continue;

            var href = node.GetAttributeValue("href", sourceUrl);
            var absoluteUrl = ToAbsoluteUrl(href, sourceUrl);
            var contextHtml = node.ParentNode?.InnerHtml ?? node.OuterHtml;

            items.Add(new ScrapedNotification
            {
                Title = title,
                SourceUrl = absoluteUrl,
                ContentHtml = contextHtml,
                NotificationType = InferType(title),
                PublishedDate = TryParseDate(node.ParentNode?.InnerText)
            });
        }

        if (items.Count == 0)
        {
            LogNoData(SourceKey);
            return [];
        }

        return items
            .GroupBy(x => x.SourceUrl, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private static ExamNotificationType InferType(string title)
    {
        if (title.Contains("admit", StringComparison.OrdinalIgnoreCase)) return ExamNotificationType.AdmitCard;
        if (title.Contains("result", StringComparison.OrdinalIgnoreCase)) return ExamNotificationType.Result;
        if (title.Contains("syllabus", StringComparison.OrdinalIgnoreCase)) return ExamNotificationType.Syllabus;
        return ExamNotificationType.Notification;
    }

    private static DateTime? TryParseDate(string? source)
    {
        if (string.IsNullOrWhiteSpace(source)) return null;
        var match = DateRegex().Match(source);
        return !match.Success ? null : DateTime.TryParse(match.Value, out var parsed) ? parsed : null;
    }

    [GeneratedRegex("(notification|admit card|result|syllabus|exam)", RegexOptions.IgnoreCase)]
    private static partial Regex SscKeywordRegex();

    [GeneratedRegex("\\b\\d{1,2}[/-]\\d{1,2}[/-]\\d{2,4}\\b")]
    private static partial Regex DateRegex();
}
