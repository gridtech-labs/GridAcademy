using GridAcademy.Data.Entities.Exam;

namespace GridAcademy.DTOs.Exam;

// ── Exam Level ──────────────────────────────────────────────────────────
public record ExamLevelDto(int Id, string Name, bool IsActive, int SortOrder, int ExamCount);
public record SaveExamLevelRequest(string Name, int SortOrder = 0, bool IsActive = true);

// ── Exam Type filter (for frontend dynamic category sidebar) ────────────
public record ExamTypeFilterDto(int Id, string Name, int ExamCount);

// ── Exam Page (list card) ───────────────────────────────────────────────
public record ExamPageCardDto(
    Guid Id,
    string Slug,
    string Title,
    string? ShortDescription,
    string? ThumbnailUrl,
    string? BannerUrl,
    string? ExamLevelName,
    string? ExamTypeName,
    string? ConductingBody,
    string? ExamCategoryName,
    string? ExamSubCategoryName,
    int  TestCount,
    bool IsFeatured,
    decimal PriceInr,
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
    string? ExamCategoryName,
    string? ExamSubCategoryName,
    bool IsFeatured,
    decimal PriceInr,
    int ViewCount,
    List<ExamTestDto> Tests,
    DateTime UpdatedAt,
    // Edit-form fields
    int? ExamLevelId,
    int? ExamTypeId,
    int? ExamCategoryId,
    int? ExamSubCategoryId,
    bool IsActive,
    ExamPageStatus Status,
    int SortOrder);

// ── Test mapped to exam ────────────────────────────────────────────────
public record ExamTestDto(
    Guid TestId,
    string Title,
    string StatusLabel,
    bool IsFree,
    int SortOrder,
    int DurationMinutes,
    int TotalQuestions);

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
    int? ExamCategoryId,
    int? ExamSubCategoryId,
    bool IsFeatured,
    bool IsActive,
    ExamPageStatus Status,
    int SortOrder,
    decimal PriceInr,
    string? MetaTitle,
    string? MetaDescription);

// ── Map / unmap test ─────────────────────────────────────────────────
public record MapTestRequest(Guid TestId, bool IsFree = true, int SortOrder = 0);
