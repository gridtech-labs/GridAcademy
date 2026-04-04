using GridAcademy.Data.Entities.Exam;
using GridAcademy.DTOs.ExamContent;
using GridAcademy.Repositories.ExamContent;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services.ExamContent;

public class ExamContentService(
    IExamContentRepository repository,
    IContentProcessingService contentProcessingService,
    ILogger<ExamContentService> logger) : IExamContentService
{
    public async Task<ExamDto> CreateExamAsync(CreateExamRequest request, CancellationToken ct = default)
    {
        var slug = contentProcessingService.GenerateSlug(request.Name);
        if (await repository.ExamSlugExistsAsync(slug, ct))
            throw new InvalidOperationException($"Exam slug '{slug}' already exists.");

        var exam = new Exam
        {
            Name = request.Name.Trim(),
            Slug = slug,
            Category = request.Category?.Trim(),
            Level = request.Level?.Trim(),
            IsActive = request.IsActive
        };

        await repository.AddExamAsync(exam, ct);
        await repository.SaveChangesAsync(ct);

        return Map(exam);
    }

    public async Task<PagedResultDto<ExamDto>> GetExamsAsync(string? category, int page, int pageSize, CancellationToken ct = default)
    {
        var query = repository.QueryExams().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(x => x.Category == category);

        var total = await query.LongCountAsync(ct);
        var items = await query.OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => Map(x))
            .ToListAsync(ct);

        return new PagedResultDto<ExamDto>(items, page, pageSize, total);
    }

    public async Task<ExamDto?> GetExamBySlugAsync(string slug, CancellationToken ct = default)
    {
        var exam = await repository.GetExamBySlugAsync(slug.ToLowerInvariant(), ct);
        return exam is null ? null : Map(exam);
    }

    public async Task<ExamNotificationDto> CreateNotificationAsync(CreateExamNotificationRequest request, CancellationToken ct = default)
    {
        ValidateSourceUrl(request.SourceUrl);

        var cleanHtml = contentProcessingService.CleanHtml(request.ContentHtml);
        var hashValue = contentProcessingService.GenerateContentHash(cleanHtml);
        if (await repository.HashExistsAsync(hashValue, ct))
            throw new InvalidOperationException("Duplicate content detected based on SHA256 hash.");

        var baseSlug = BuildNotificationSlug(request.Title, request.NotificationType);
        var slug = await EnsureUniqueNotificationSlug(baseSlug, ct);
        var summary = contentProcessingService.ExtractSummary(cleanHtml);

        var notification = new ExamNotification
        {
            ExamId = request.ExamId,
            Title = request.Title.Trim(),
            Slug = slug,
            ContentHtml = cleanHtml,
            Summary = summary,
            NotificationType = request.NotificationType,
            ImportantDates = request.ImportantDates,
            SourceUrl = request.SourceUrl.Trim(),
            CanonicalUrl = string.IsNullOrWhiteSpace(request.CanonicalUrl) ? $"/notifications/{slug}" : request.CanonicalUrl.Trim(),
            MetaTitle = string.IsNullOrWhiteSpace(request.MetaTitle) ? request.Title.Trim() : request.MetaTitle.Trim(),
            MetaDescription = string.IsNullOrWhiteSpace(request.MetaDescription) ? summary : request.MetaDescription.Trim(),
            Status = PublicationStatus.Draft,
            PublishedAt = null
        };

        await repository.AddNotificationAsync(notification, ct);
        await repository.AddHashAsync(new ContentHash { HashValue = hashValue, SourceUrl = request.SourceUrl.Trim() }, ct);
        await repository.AddVersionAsync(new ContentVersion { EntityType = nameof(ExamNotification), EntityId = notification.Id, ContentHtml = cleanHtml }, ct);
        await repository.SaveChangesAsync(ct);

        logger.LogInformation("Exam notification created. ExamId: {ExamId}, NotificationId: {NotificationId}, Slug: {Slug}", notification.ExamId, notification.Id, notification.Slug);

        var persisted = await repository.GetNotificationByIdAsync(notification.Id, ct)
            ?? throw new InvalidOperationException("Created notification not found.");
        return Map(persisted);
    }

    public async Task<ExamNotificationDto?> GetNotificationBySlugAsync(string slug, CancellationToken ct = default)
    {
        var item = await repository.GetNotificationBySlugAsync(slug.ToLowerInvariant(), ct);
        return item is null ? null : Map(item);
    }

    public async Task<PagedResultDto<ExamNotificationDto>> GetNotificationsAsync(Guid? examId, string? category, ExamNotificationType? type, int page, int pageSize, CancellationToken ct = default)
    {
        var query = repository.QueryNotifications().Where(x => x.Status == PublicationStatus.Published && x.Exam!.IsActive);

        if (examId.HasValue) query = query.Where(x => x.ExamId == examId.Value);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Exam!.Category == category);
        if (type.HasValue) query = query.Where(x => x.NotificationType == type.Value);

        var total = await query.LongCountAsync(ct);
        var items = await query.OrderByDescending(x => x.PublishedAt).ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => Map(x))
            .ToListAsync(ct);

        return new PagedResultDto<ExamNotificationDto>(items, page, pageSize, total);
    }

    public async Task<ExamNotificationDto> UpdateNotificationAsync(Guid id, UpdateExamNotificationRequest request, CancellationToken ct = default)
    {
        ValidateSourceUrl(request.SourceUrl);

        var notification = await repository.GetNotificationByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("Notification not found.");

        var cleanHtml = contentProcessingService.CleanHtml(request.ContentHtml);
        var newHash = contentProcessingService.GenerateContentHash(cleanHtml);
        var currentHash = contentProcessingService.GenerateContentHash(notification.ContentHtml);
        if (newHash != currentHash && await repository.HashExistsAsync(newHash, ct))
            throw new InvalidOperationException("Duplicate content detected based on SHA256 hash.");

        notification.Title = request.Title.Trim();
        notification.ContentHtml = cleanHtml;
        notification.Summary = contentProcessingService.ExtractSummary(cleanHtml);
        notification.NotificationType = request.NotificationType;
        notification.ImportantDates = request.ImportantDates;
        notification.SourceUrl = request.SourceUrl.Trim();
        notification.CanonicalUrl = string.IsNullOrWhiteSpace(request.CanonicalUrl) ? $"/notifications/{notification.Slug}" : request.CanonicalUrl.Trim();
        notification.MetaTitle = string.IsNullOrWhiteSpace(request.MetaTitle) ? notification.Title : request.MetaTitle.Trim();
        notification.MetaDescription = string.IsNullOrWhiteSpace(request.MetaDescription) ? notification.Summary : request.MetaDescription.Trim();
        notification.UpdatedAt = DateTime.UtcNow;

        await repository.AddVersionAsync(new ContentVersion { EntityType = nameof(ExamNotification), EntityId = notification.Id, ContentHtml = cleanHtml }, ct);
        if (newHash != currentHash)
            await repository.AddHashAsync(new ContentHash { HashValue = newHash, SourceUrl = notification.SourceUrl }, ct);
        await repository.SaveChangesAsync(ct);

        return Map(notification);
    }

    public async Task<ExamNotificationDto> ChangeStatusAsync(Guid id, PublicationStatus status, CancellationToken ct = default)
    {
        var notification = await repository.GetNotificationByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("Notification not found.");

        var currentStatus = notification.Status;
        var isValid = (currentStatus, status) switch
        {
            (PublicationStatus.Draft, PublicationStatus.AIProcessed) => true,
            (PublicationStatus.AIProcessed, PublicationStatus.Approved) => true,
            (PublicationStatus.Approved, PublicationStatus.Published) => true,
            _ => false
        };

        if (!isValid)
            throw new InvalidOperationException($"Invalid status transition from {currentStatus} to {status}.");

        notification.Status = status;
        notification.PublishedAt = status == PublicationStatus.Published ? DateTime.UtcNow : null;
        notification.UpdatedAt = DateTime.UtcNow;
        await repository.SaveChangesAsync(ct);

        return Map(notification);
    }

    public async Task<IReadOnlyList<ContentVersionDto>> GetVersionHistoryAsync(Guid notificationId, CancellationToken ct = default)
    {
        var versions = await repository.GetVersionsAsync(nameof(ExamNotification), notificationId, ct);
        return versions.Select(v => new ContentVersionDto(v.Id, v.EntityType, v.EntityId, v.ContentHtml, v.CreatedAt)).ToList();
    }

    private async Task<string> EnsureUniqueNotificationSlug(string baseSlug, CancellationToken ct)
    {
        var candidate = baseSlug;
        var suffix = 1;
        while (await repository.QueryNotifications().AnyAsync(x => x.Slug == candidate, ct))
        {
            suffix++;
            candidate = $"{baseSlug}-{suffix}";
        }

        return candidate;
    }

    private string BuildNotificationSlug(string title, ExamNotificationType type)
    {
        var slugBase = contentProcessingService.GenerateSlug(title);
        return $"{slugBase}-{type.ToString().ToLowerInvariant()}-{DateTime.UtcNow.Year}";
    }

    private static void ValidateSourceUrl(string sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl))
            throw new InvalidOperationException("SourceUrl is mandatory.");
    }

    private static ExamDto Map(Exam exam) =>
        new(exam.Id, exam.Name, exam.Slug, exam.Category, exam.Level, exam.IsActive, exam.CreatedAt);

    private static ExamNotificationDto Map(ExamNotification notification) =>
        new(
            notification.Id,
            notification.ExamId ?? Guid.Empty,
            notification.Exam?.Name ?? string.Empty,
            notification.Title,
            notification.Slug,
            notification.ContentHtml,
            notification.Summary,
            notification.NotificationType,
            notification.ImportantDates,
            notification.SourceUrl,
            notification.CanonicalUrl,
            notification.MetaTitle,
            notification.MetaDescription,
            notification.Status,
            notification.PublishedAt,
            notification.CreatedAt);
}
