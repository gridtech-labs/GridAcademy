using GridAcademy.Data.Entities.VideoLearning;

namespace GridAcademy.DTOs.VideoLearning;

// ── Domain ─────────────────────────────────────────────────────
public record DomainDto(int Id, string Name, string? Description, string? LogoUrl, bool IsActive, int SortOrder, int VideoCount);
public record CreateDomainRequest(string Name, string? Description, int SortOrder = 0, bool IsActive = true);

// ── Video Category ─────────────────────────────────────────────
public record VideoCategoryDto(int Id, int DomainId, string DomainName, string Name, bool IsActive, int SortOrder, int VideoCount);
public record CreateVideoCategoryRequest(int DomainId, string Name, int SortOrder = 0, bool IsActive = true);

// ── Video ──────────────────────────────────────────────────────
public record VideoDto(
    Guid Id, int DomainId, string DomainName, int CategoryId, string CategoryName,
    string Title, string? Description, string? FilePath, string? ThumbnailPath,
    int DurationSeconds, bool IsFreePreview, VideoStatus Status,
    int SortOrder, long FileSizeBytes, string? OriginalFileName,
    DateTime CreatedAt, DateTime UpdatedAt);

public record CreateVideoRequest(
    int DomainId, int CategoryId, string Title, string? Description,
    bool IsFreePreview = false, int SortOrder = 0, VideoStatus Status = VideoStatus.Draft);

public record UpdateVideoRequest(
    int CategoryId, string Title, string? Description,
    bool IsFreePreview, int SortOrder, VideoStatus Status);

public record VideoListRequest(
    int? DomainId, int? CategoryId, VideoStatus? Status,
    string? Search, int Page = 1, int PageSize = 20);

// ── Learning Path ──────────────────────────────────────────────
public record LearningPathDto(
    Guid Id, int DomainId, string DomainName, string Title, string? Description,
    string? ThumbnailPath, bool IsActive, int SortOrder,
    int NodeCount, int ModuleCount, DateTime CreatedAt);

public record CreateLearningPathRequest(int DomainId, string Title, string? Description, bool IsActive = true, int SortOrder = 0);
public record UpdateLearningPathRequest(string Title, string? Description, bool IsActive, int SortOrder);
public record ReorderItem(Guid ItemId, int SortOrder);

// ── Program ────────────────────────────────────────────────────
public record PricingPlanDto(
    int Id, Guid ProgramId, string Name,
    decimal PriceInr, decimal PriceUsd,
    decimal? OriginalPriceInr, decimal? OriginalPriceUsd,
    int? ValidityDays, bool IsActive, int SortOrder);

public record CreatePricingPlanRequest(
    string Name, decimal PriceInr, decimal PriceUsd,
    decimal? OriginalPriceInr, decimal? OriginalPriceUsd,
    int? ValidityDays, bool IsActive = true, int SortOrder = 0);

public record ProgramSummaryDto(
    Guid Id, int DomainId, string DomainName, string Title, string? ShortDescription,
    string? ThumbnailPath, ProgramStatus Status, int LearningPathCount,
    int PricingPlanCount, DateTime CreatedAt);

public record ProgramDto(
    Guid Id, int DomainId, string DomainName, string Title, string? ShortDescription,
    string? Description, string? ThumbnailPath, ProgramStatus Status,
    List<PricingPlanDto> PricingPlans,
    List<LearningPathDto> LearningPaths,
    DateTime CreatedAt, DateTime UpdatedAt);

public record CreateProgramRequest(
    int DomainId, string Title, string? LearningCode, string? ShortDescription, string? Description,
    bool IsBlendedLearning = false,
    ProgramStatus Status = ProgramStatus.Draft,
    List<CreatePricingPlanRequest>? Plans = null,
    List<Guid>? LearningPathIds = null);

public record UpdateProgramRequest(
    int DomainId, string Title, string? LearningCode, string? ShortDescription, string? Description,
    bool IsBlendedLearning, ProgramStatus Status);

public record ProgramListRequest(
    int? DomainId, ProgramStatus? Status, string? Search,
    int Page = 1, int PageSize = 20);

// ── Coupon ─────────────────────────────────────────────────────
public record CouponDto(
    int Id, string Code, string? Description, DiscountType DiscountType,
    decimal DiscountValue, decimal? MaxDiscountInr, decimal? MaxDiscountUsd,
    DateTime? ValidFrom, DateTime? ValidTo, int? UsageLimit, int UsedCount,
    Guid? ProgramId, string? ProgramTitle, bool IsActive);

public record CreateCouponRequest(
    string Code, string? Description, DiscountType DiscountType, decimal DiscountValue,
    decimal? MaxDiscountInr, decimal? MaxDiscountUsd,
    DateTime? ValidFrom, DateTime? ValidTo, int? UsageLimit,
    Guid? ProgramId, bool IsActive = true);

public record UpdateCouponRequest(
    string? Description, DiscountType DiscountType, decimal DiscountValue,
    decimal? MaxDiscountInr, decimal? MaxDiscountUsd,
    DateTime? ValidFrom, DateTime? ValidTo, int? UsageLimit,
    Guid? ProgramId, bool IsActive);

public record CouponListRequest(Guid? ProgramId, bool? IsActive, string? Search, int Page = 1, int PageSize = 20);

public record CouponValidationResult(bool IsValid, string? Error, decimal DiscountInr, decimal DiscountUsd);

// ── Sales Channel ──────────────────────────────────────────────
public record SalesChannelDto(int Id, string Name, string? BaseUrl, bool IsActive, DateTime CreatedAt);
public record CreateSalesChannelRequest(string Name, string? BaseUrl, bool IsActive = true);
public record UpdateSalesChannelRequest(string Name, string? BaseUrl, bool IsActive);
public record CreateChannelResult(SalesChannelDto Channel, string RawApiKey);

public record ChannelPriceDto(int Id, int ChannelId, int PricingPlanId, string PlanName,
    string ProgramTitle, decimal DefaultPriceInr, decimal DefaultPriceUsd,
    decimal? OverridePriceInr, decimal? OverridePriceUsd, bool IsActive);
public record SetChannelPriceRequest(decimal? OverridePriceInr, decimal? OverridePriceUsd, bool IsActive = true);

// ── Enrollment ─────────────────────────────────────────────────
public record EnrollmentDto(
    Guid Id, Guid UserId, string UserEmail, Guid ProgramId, string ProgramTitle,
    int PricingPlanId, string PlanName,
    EnrollmentStatus Status, decimal AmountPaidInr, decimal AmountPaidUsd,
    string? CouponCode, decimal? DiscountApplied,
    int? ChannelId, string? ChannelName,
    DateTime EnrolledAt, DateTime? ExpiresAt);

public record CreateEnrollmentRequest(
    Guid UserId, Guid ProgramId, int PricingPlanId,
    decimal AmountPaidInr, decimal AmountPaidUsd,
    string? CouponCode, decimal? DiscountApplied,
    int? ChannelId, DateTime? ExpiresAt);

public record EnrollmentListRequest(
    Guid? UserId, Guid? ProgramId, EnrollmentStatus? Status,
    string? UserEmail, DateTime? FromDate, DateTime? ToDate,
    int Page = 1, int PageSize = 20);

public record VideoProgressDto(
    Guid Id, Guid EnrollmentId, Guid VideoId, string VideoTitle,
    VideoProgressStatus Status, int WatchedSeconds, DateTime? CompletedAt);

public record EnrollmentProgressSummaryDto(
    int TotalVideos, int CompletedVideos, int InProgressVideos,
    int NotStartedVideos, decimal PercentComplete);

// ── Content File ───────────────────────────────────────────────
public record ContentFileDto(
    Guid Id, int DomainId, string DomainName, string Title, string? Description,
    ContentFileType ContentType, string? FilePath, string? OriginalFileName,
    long FileSizeBytes, bool IsActive, DateTime CreatedAt);

public record CreateContentFileRequest(
    int DomainId, string Title, string? Description,
    ContentFileType ContentType, bool IsActive = true);

public record ContentFileListRequest(int? DomainId, ContentFileType? ContentType, string? Search, int Page = 1, int PageSize = 20);

// ── Learning Path Node ─────────────────────────────────────────
/// <summary>
/// NodeType codes: N=Module, AS=Assessment, VL=Video, SC=SCORM, PD=PDF, HT=HTML
/// </summary>
public record LpNodeDto(
    int Id, Guid LearningPathId, int? ParentNodeId,
    string NodeType, string Title,
    Guid? ContentId, bool IsPreview, int SortOrder, bool IsActive,
    List<LpNodeDto> Children);

public record CreateLpModuleRequest(string Title, int SortOrder = 0);

public record CreateLpContentRequest(
    int? ParentNodeId,   // null = top-level content (no module)
    string NodeType,     // AS | VL | SC | PD | HT
    Guid ContentId,
    string Title,
    bool IsPreview = false,
    int SortOrder = 0);

/// <summary>Multiple content items added in one request (e.g. multi-select assessments).</summary>
public record AddLpContentBatchRequest(
    int? ParentNodeId,
    string NodeType,
    List<Guid> ContentIds,   // one node created per ID
    bool IsPreview = false);

// ── Learning Path Detail ───────────────────────────────────────
public record LearningPathDetailDto(
    Guid Id, int DomainId, string DomainName, string Title, string? Description,
    string? ThumbnailPath, bool IsActive, int SortOrder,
    int NodeCount, DateTime CreatedAt,
    List<LpNodeDto> TopLevelNodes);  // modules (type=N) + direct content

// ── Course Launch ──────────────────────────────────────────────
public record CourseLaunchDto(
    int Id, Guid ProgramId, string ProgramName, string Name,
    CourseLaunchStatus Status, string? BlockedReason,
    DateTime? StartDate, DateTime? EndDate, int MaxEnrollments,
    DateTime CreatedAt);

public record CreateCourseLaunchRequest(
    string Name, CourseLaunchStatus Status = CourseLaunchStatus.Active,
    string? BlockedReason = null,
    DateTime? StartDate = null, DateTime? EndDate = null,
    int MaxEnrollments = 0);

// ── Content Picker (AJAX response) ────────────────────────────
public record ContentPickerItem(Guid Id, string Title, string SubTitle, int? DurationSeconds);
