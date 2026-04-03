using GridAcademy.Data.Entities.Exam;

namespace GridAcademy.DTOs.ExamContent;

public record PagedResultDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalRecords);

public record CreateExamRequest(string Name, string? Category, string? Level, bool IsActive = true);
public record ExamDto(Guid Id, string Name, string Slug, string? Category, string? Level, bool IsActive, DateTime CreatedAt);

public record CreateExamNotificationRequest(
    Guid ExamId,
    string Title,
    string ContentHtml,
    string? ImportantDates,
    ExamNotificationType NotificationType,
    string SourceUrl,
    string? CanonicalUrl,
    string? MetaTitle,
    string? MetaDescription);

public record UpdateExamNotificationRequest(
    string Title,
    string ContentHtml,
    string? ImportantDates,
    ExamNotificationType NotificationType,
    string SourceUrl,
    string? CanonicalUrl,
    string? MetaTitle,
    string? MetaDescription);

public record ExamNotificationDto(
    Guid Id,
    Guid ExamId,
    string ExamName,
    string Title,
    string Slug,
    string ContentHtml,
    string? Summary,
    ExamNotificationType NotificationType,
    string? ImportantDates,
    string SourceUrl,
    string? CanonicalUrl,
    string? MetaTitle,
    string? MetaDescription,
    PublicationStatus Status,
    DateTime? PublishedAt,
    DateTime CreatedAt);

public record ContentVersionDto(Guid Id, string EntityType, Guid EntityId, string ContentHtml, DateTime CreatedAt);
