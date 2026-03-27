using GridAcademy.Data.Entities.Exam;

namespace GridAcademy.DTOs.Exam;

// ── Exam Level ──────────────────────────────────────────────────────────
public record ExamLevelDto(int Id, string Name, bool IsActive, int SortOrder, int ExamCount);
public record SaveExamLevelRequest(string Name, int SortOrder = 0, bool IsActive = true);

// ── Exam Page (list card) ───────────────────────────────────────────────
public record ExamPageCardDto(
    Guid Id,
    string Slug,
    string Title,
    string? ShortDescription,
    string? ThumbnailUrl,
    string? ExamLevelName,
    string? ExamTypeName,
    string? ConductingBody,
    int  TestCount,
    bool IsFeatured,
    ExamPageStatus Status,
    DateTime CreatedAt);

// ── Exam Page (full detail for public view) ────────────────────────────
public record ExamPageDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string? ShortDescription,
    string? Overview,
    string? Eligibility,
    string? Syllabus,
    string? ExamPattern,
    string? ImportantDates,
    string? AdmitCard,
    string? ResultInfo,
    string? CutOff,
    string? HowToApply,
    string? ConductingBody,
    string? OfficialWebsite,
    string? NotificationUrl,
    string? ThumbnailUrl,
    string? BannerUrl,
    string? ExamLevelName,
    string? ExamTypeName,
    string? MetaTitle,
    string? MetaDescription,
    bool IsFeatured,
    int ViewCount,
    List<ExamTestDto> Tests,
    DateTime UpdatedAt);

// ── Test mapped to exam ────────────────────────────────────────────────
public record ExamTestDto(
    Guid TestId,
    string Title,
    string StatusLabel,
    bool IsFree,
    int SortOrder);

// ── Save exam page (admin create/edit) ────────────────────────────────
public record SaveExamPageRequest(
    string Title,
    string Slug,
    string? ShortDescription,
    string? Overview,
    string? Eligibility,
    string? Syllabus,
    string? ExamPattern,
    string? ImportantDates,
    string? AdmitCard,
    string? ResultInfo,
    string? CutOff,
    string? HowToApply,
    string? ConductingBody,
    string? OfficialWebsite,
    string? NotificationUrl,
    string? ThumbnailUrl,
    string? BannerUrl,
    int? ExamLevelId,
    int? ExamTypeId,
    bool IsFeatured,
    bool IsActive,
    ExamPageStatus Status,
    int SortOrder,
    string? MetaTitle,
    string? MetaDescription);

// ── Map / unmap test ─────────────────────────────────────────────────
public record MapTestRequest(Guid TestId, bool IsFree = true, int SortOrder = 0);
