using HtmlAgilityPack;

namespace GridAcademy.Services.ExamContent.Scraping.Scrapers;

public abstract class BaseHtmlScraper(HttpClient httpClient, ILogger logger)
{
    protected async Task<HtmlDocument> LoadDocumentAsync(string url, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(ct);

        var document = new HtmlDocument();
        document.LoadHtml(html);
        return document;
    }

    protected static string ToAbsoluteUrl(string url, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(url)) return baseUrl;
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute)) return absolute.ToString();
        return new Uri(new Uri(baseUrl), url).ToString();
    }

    protected static string Normalize(string? value) => HtmlEntity.DeEntitize(value ?? string.Empty).Trim();

    protected void LogNoData(string sourceKey) => logger.LogWarning("Scraper {SourceKey} returned no records.", sourceKey);
}
