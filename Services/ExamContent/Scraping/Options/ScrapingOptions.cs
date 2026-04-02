namespace GridAcademy.Services.ExamContent.Scraping.Options;

public class ScrapingOptions
{
    public const string SectionName = "Scraping";

    public int IntervalHours { get; set; } = 6;
    public Dictionary<string, string> Sources { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
