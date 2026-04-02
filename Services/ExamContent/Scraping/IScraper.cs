using GridAcademy.Services.ExamContent.Scraping.Models;

namespace GridAcademy.Services.ExamContent.Scraping;

public interface IScraper
{
    string SourceKey { get; }
    Task<List<ScrapedNotification>> FetchAsync(CancellationToken ct = default);
}
