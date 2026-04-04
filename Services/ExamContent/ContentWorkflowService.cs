using GridAcademy.Data;
using GridAcademy.Data.Entities.Exam;
using GridAcademy.DTOs.ExamContent;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services.ExamContent;

public class ContentWorkflowService(
    AppDbContext db,
    ILogger<ContentWorkflowService> logger) : IContentWorkflowService
{
    public async Task<ContentActionResponseDto> ApproveAsync(Guid id, CancellationToken ct = default)
    {
        var notification = await db.ExamNotifications.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Content not found.");

        if (notification.Status != PublicationStatus.AIProcessed)
        {
            logger.LogWarning(
                "Invalid approval transition for NotificationId={NotificationId}. CurrentStatus={CurrentStatus}",
                id,
                notification.Status);
            throw new InvalidOperationException($"Only {PublicationStatus.AIProcessed} content can be approved.");
        }

        notification.Status = PublicationStatus.Approved;
        notification.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Content approved. NotificationId={NotificationId}", id);
        return new ContentActionResponseDto(true, PublicationStatus.Approved.ToString());
    }

    public async Task<ContentActionResponseDto> PublishAsync(Guid id, CancellationToken ct = default)
    {
        var notification = await db.ExamNotifications.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Content not found.");

        if (notification.Status != PublicationStatus.Approved)
        {
            logger.LogWarning(
                "Invalid publish transition for NotificationId={NotificationId}. CurrentStatus={CurrentStatus}",
                id,
                notification.Status);
            throw new InvalidOperationException($"Only {PublicationStatus.Approved} content can be published.");
        }

        notification.Status = PublicationStatus.Published;
        notification.PublishedAt = DateTime.UtcNow;
        notification.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Content published. NotificationId={NotificationId}, PublishedAt={PublishedAt}", id, notification.PublishedAt);
        return new ContentActionResponseDto(true, PublicationStatus.Published.ToString());
    }

    public async Task<PublicContentDto?> GetPublishedBySlugAsync(string slug, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        var record = await db.ExamNotifications
            .AsNoTracking()
            .Where(x => x.Slug == slug.ToLowerInvariant() && x.Status == PublicationStatus.Published && x.PublishedAt != null)
            .Select(x => new PublicContentDto(
                x.Title,
                x.Slug,
                x.ContentHtml,
                x.MetaTitle,
                x.MetaDescription,
                x.PublishedAt!.Value))
            .FirstOrDefaultAsync(ct);

        return record;
    }

    public async Task<AdminContentListResponseDto> GetAdminContentAsync(PublicationStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 200);

        var query = db.ExamNotifications.AsNoTracking();
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var total = await query.LongCountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(x => new AdminContentListItemDto(
                x.Id,
                x.Title,
                x.Slug,
                x.Status,
                x.IsAiProcessed,
                x.CreatedAt,
                x.UpdatedAt,
                x.PublishedAt))
            .ToListAsync(ct);

        return new AdminContentListResponseDto(items, safePage, safePageSize, total);
    }
}
