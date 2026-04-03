using GridAcademy.Data.Entities.Exam;

namespace GridAcademy.DTOs.ExamContent;

public record ContentActionResponseDto(bool Success, string Status);

public record AdminContentListItemDto(
    Guid Id,
    string Title,
    string Slug,
    PublicationStatus Status,
    bool IsAiProcessed,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? PublishedAt);

public record AdminContentListResponseDto(
    IReadOnlyList<AdminContentListItemDto> Items,
    int Page,
    int PageSize,
    long TotalRecords);

public record PublicContentDto(
    string Title,
    string Slug,
    string ContentHtml,
    string? MetaTitle,
    string? MetaDescription,
    DateTime PublishedAt);
