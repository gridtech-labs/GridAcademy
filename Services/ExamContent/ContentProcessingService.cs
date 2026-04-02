using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using GridAcademy.Data.Entities.Exam;
using GridAcademy.Repositories.ExamContent;
using GridAcademy.Services.ExamContent.Scraping.Models;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services.ExamContent;

public partial class ContentProcessingService(
    IExamContentRepository repository,
    ILogger<ContentProcessingService> logger) : IContentProcessingService
{
    public string CleanHtml(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent)) return string.Empty;
        var withoutScripts = ScriptsRegex().Replace(htmlContent, string.Empty);
        var withoutStyle = StyleRegex().Replace(withoutScripts, string.Empty);
        return withoutStyle.Trim();
    }

    public string ExtractSummary(string htmlContent, int wordLimit = 150)
    {
        var normalized = NormalizeForHash(htmlContent);
        if (string.IsNullOrWhiteSpace(normalized)) return string.Empty;
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= wordLimit) return normalized;
        return string.Join(' ', words.Take(wordLimit)) + "...";
    }

    public string GenerateSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var value = WebUtility.HtmlDecode(input).ToLowerInvariant();
        value = NonAlphaNumericRegex().Replace(value, "-");
        value = MultiDashRegex().Replace(value, "-").Trim('-');
        return value;
    }

    public string GenerateContentHash(string htmlContent)
    {
        var normalized = NormalizeForHash(htmlContent);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public string NormalizeForHash(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent)) return string.Empty;
        var noHtml = HtmlTagRegex().Replace(htmlContent, " ");
        var decoded = WebUtility.HtmlDecode(noHtml);
        var normalizedWhitespace = MultiWhitespaceRegex().Replace(decoded, " ").Trim();
        return normalizedWhitespace.ToLowerInvariant();
    }

    public async Task ProcessAsync(ScrapedNotification notification, string? preComputedHash = null, CancellationToken ct = default)
    {
        var exam = await ResolveExamAsync(notification.SourceUrl, ct);
        var cleanHtml = CleanHtml(notification.ContentHtml);
        var hashValue = preComputedHash ?? GenerateContentHash(cleanHtml);

        var baseSlug = $"{GenerateSlug(notification.Title)}-{notification.NotificationType.ToString().ToLowerInvariant()}-{DateTime.UtcNow:yyyy}";
        var slug = await EnsureUniqueNotificationSlugAsync(baseSlug, ct);
        var summary = ExtractSummary(cleanHtml);

        var entity = new ExamNotification
        {
            ExamId = exam.Id,
            Title = notification.Title,
            Slug = slug,
            ContentHtml = cleanHtml,
            Summary = summary,
            NotificationType = notification.NotificationType,
            SourceUrl = notification.SourceUrl,
            CanonicalUrl = notification.SourceUrl,
            MetaTitle = notification.Title,
            MetaDescription = summary,
            Status = PublicationStatus.Published,
            PublishedAt = notification.PublishedDate ?? DateTime.UtcNow
        };

        await repository.AddNotificationAsync(entity, ct);
        await repository.AddVersionAsync(new ContentVersion
        {
            EntityType = nameof(ExamNotification),
            EntityId = entity.Id,
            ContentHtml = cleanHtml
        }, ct);
        await repository.AddHashAsync(new ContentHash
        {
            HashValue = hashValue,
            SourceUrl = notification.SourceUrl
        }, ct);

        await repository.SaveChangesAsync(ct);
        logger.LogInformation("Processed scraped notification for {SourceUrl} into exam {ExamSlug}.", notification.SourceUrl, exam.Slug);
    }

    private async Task<Exam> ResolveExamAsync(string sourceUrl, CancellationToken ct)
    {
        var examSlug = ResolveExamSlug(sourceUrl);
        var exam = await repository.QueryExams().FirstOrDefaultAsync(x => x.Slug == examSlug, ct);
        if (exam is not null) return exam;

        var created = new Exam
        {
            Name = ResolveExamName(examSlug),
            Slug = examSlug,
            Category = "Government",
            Level = "National",
            IsActive = true
        };
        await repository.AddExamAsync(created, ct);
        await repository.SaveChangesAsync(ct);
        return created;
    }

    private async Task<string> EnsureUniqueNotificationSlugAsync(string slugBase, CancellationToken ct)
    {
        var candidate = slugBase;
        var suffix = 1;
        while (await repository.QueryNotifications().AnyAsync(x => x.Slug == candidate, ct))
        {
            suffix++;
            candidate = $"{slugBase}-{suffix}";
        }

        return candidate;
    }

    private static string ResolveExamSlug(string sourceUrl)
    {
        var lower = sourceUrl.ToLowerInvariant();
        if (lower.Contains("ssc")) return "ssc";
        if (lower.Contains("upsc")) return "upsc";
        if (lower.Contains("rrb") || lower.Contains("railway")) return "railway";
        if (lower.Contains("bank")) return "banking";
        return "state-exams";
    }

    private static string ResolveExamName(string slug) => slug switch
    {
        "ssc" => "SSC",
        "upsc" => "UPSC",
        "railway" => "Railway",
        "banking" => "Banking",
        _ => "State Exams"
    };

    [GeneratedRegex("<script[\\s\\S]*?</script>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptsRegex();

    [GeneratedRegex("<style[\\s\\S]*?</style>", RegexOptions.IgnoreCase)]
    private static partial Regex StyleRegex();

    [GeneratedRegex("<[^>]+>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.IgnoreCase)]
    private static partial Regex NonAlphaNumericRegex();

    [GeneratedRegex("-+")]
    private static partial Regex MultiDashRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex MultiWhitespaceRegex();
}
