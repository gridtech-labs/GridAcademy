using GridAcademy.Data.Entities.Exam;
using GridAcademy.Repositories.ExamContent;
using GridAcademy.Services.ExamContent.Models;
using GridAcademy.Services.ExamContent.Scraping.Models;
using GridAcademy.Services.ExamContent.Utilities;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services.ExamContent;

public class ContentProcessingService(
    IExamContentRepository repository,
    ILogger<ContentProcessingService> logger) : IContentProcessingService
{
    public string CleanHtml(string htmlContent) => HtmlCleaner.Clean(htmlContent);

    public string ExtractSummary(string cleanedText, int wordLimit = 180) => BuildSummary(cleanedText, wordLimit);

    public string GenerateSlug(string input) => SlugHelper.Generate(input);

    public string GenerateContentHash(string content) => HashHelper.ComputeSha256(content);

    public async Task<ContentProcessingResult> ProcessAsync(ScrapedNotification notification, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Processing scraped notification: {Title} ({SourceUrl})", notification.Title, notification.SourceUrl);

            await repository.EnsureExamContentSchemaAsync(ct);

            var cleanedText = CleanHtml(notification.ContentHtml);
            var contentHash = GenerateContentHash(cleanedText);

            if (await repository.HashExistsAsync(contentHash, ct))
            {
                logger.LogInformation("Duplicate skipped for source {SourceUrl} with hash {Hash}", notification.SourceUrl, contentHash);
                return ContentProcessingResult.Duplicate(contentHash);
            }

            var slugBase = GenerateSlug(notification.Title);
            var uniqueSlug = await EnsureUniqueSlugAsync(slugBase, ct);
            var summary = ExtractSummary(cleanedText, 180);
            var mappedExamId = await TryMapExamIdAsync(notification.Title, ct);

            var entity = new ExamNotification
            {
                ExamId = mappedExamId,
                Title = notification.Title,
                Slug = uniqueSlug,
                ContentHtml = notification.ContentHtml,
                Summary = summary,
                SourceUrl = notification.SourceUrl,
                CanonicalUrl = notification.SourceUrl,
                NotificationType = notification.NotificationType,
                Status = PublicationStatus.Draft,
                PublishedAt = notification.PublishedDate
            };

            await repository.AddNotificationAsync(entity, ct);
            await repository.AddHashAsync(new ContentHash
            {
                HashValue = contentHash,
                SourceUrl = notification.SourceUrl
            }, ct);
            await repository.SaveChangesAsync(ct);

            logger.LogInformation("Saved draft notification {Slug} for source {SourceUrl}", entity.Slug, entity.SourceUrl);
            return ContentProcessingResult.Saved(entity.Slug);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error while processing scraped notification for {SourceUrl}", notification.SourceUrl);
            return ContentProcessingResult.Failed("Database write failed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error while processing scraped notification for {SourceUrl}", notification.SourceUrl);
            return ContentProcessingResult.Failed(ex.Message);
        }
    }

    private async Task<string> EnsureUniqueSlugAsync(string slugBase, CancellationToken ct)
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

    private async Task<Guid?> TryMapExamIdAsync(string title, CancellationToken ct)
    {
        var normalized = title.ToLowerInvariant();
        var examSlug = normalized switch
        {
            var t when t.Contains("ssc") => "ssc",
            var t when t.Contains("upsc") => "upsc",
            var t when t.Contains("rrb") || t.Contains("railway") => "railway",
            var t when t.Contains("bank") || t.Contains("ibps") || t.Contains("sbi") => "banking",
            _ => null
        };

        if (examSlug is null)
        {
            return null;
        }

        var exam = await repository.GetExamBySlugAsync(examSlug, ct);
        return exam?.Id;
    }

    private static string BuildSummary(string cleanedText, int wordLimit)
    {
        if (string.IsNullOrWhiteSpace(cleanedText)) return string.Empty;

        var words = cleanedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= wordLimit) return cleanedText;

        return string.Join(' ', words.Take(wordLimit)) + "...";
    }
}
