using System.ComponentModel.DataAnnotations;
using GridAcademy.Data.Entities.Marketplace;

namespace GridAcademy.DTOs.Marketplace;

// ─────────────────────────────────────────────────────────────────────────────
// AUTH — OTP + Student/Provider registration
// ─────────────────────────────────────────────────────────────────────────────

public record SendOtpRequest(
    [Required, MaxLength(200)] string Contact   // mobile or email
);

public record VerifyOtpRequest(
    [Required, MaxLength(200)] string Contact,
    [Required, MaxLength(6)]   string OtpCode
);

public record StudentRegisterRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, EmailAddress, MaxLength(200)] string Email,
    [MaxLength(15)] string? Phone,
    [Required, MinLength(8)] string Password
);

public record ProviderRegisterRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, EmailAddress, MaxLength(200)] string Email,
    [MaxLength(15)] string? Phone,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(200)] string InstituteName,
    [MaxLength(100)] string? City,
    [MaxLength(100)] string? State,
    [MaxLength(500)] string? Bio,
    bool AgreedToTerms = false
);

// ─────────────────────────────────────────────────────────────────────────────
// PROVIDER
// ─────────────────────────────────────────────────────────────────────────────

public record ProviderDto(
    int        Id,
    Guid       UserId,
    string     InstituteName,
    string?    City,
    string?    State,
    string?    Bio,
    string?    LogoUrl,
    string     Status,         // "Pending" | "Verified" | "Suspended"
    bool       AgreedToTerms,
    DateTime   CreatedAt
);

/// <summary>Profile update — sensitive banking fields excluded (handled separately).</summary>
public record UpdateProviderProfileRequest(
    [Required, MaxLength(200)] string InstituteName,
    [MaxLength(100)] string? City,
    [MaxLength(100)] string? State,
    [MaxLength(500)] string? Bio,
    [MaxLength(300)] string? LogoUrl
);

public record ProviderBankingRequest(
    [Required, MaxLength(20)]  string PanNumber,
    [Required, MaxLength(20)]  string BankAccount,
    [Required, MaxLength(11)]  string IfscCode,
    [Required, MaxLength(200)] string AccountHolderName
);

public record ProviderDashboardDto(
    int     TotalSeries,
    int     PublishedSeries,
    int     DraftSeries,
    int     PendingReviewSeries,
    int     TotalSales,
    decimal TotalRevenue,
    decimal PendingPayout,
    decimal ProcessedPayout,
    IReadOnlyList<ProviderSeriesSummaryDto> TopSeries
);

public record ProviderSeriesSummaryDto(
    Guid    Id,
    string  Title,
    string  Status,
    int     PurchaseCount,
    decimal AvgRating,
    decimal Revenue
);

// ─────────────────────────────────────────────────────────────────────────────
// TEST SERIES
// ─────────────────────────────────────────────────────────────────────────────

public record CreateTestSeriesRequest(
    [Required, MaxLength(300)] string Title,
    [Required] int ExamTypeId,
    SeriesType SeriesType,
    [MaxLength(500)] string? ShortDescription,
    string? FullDescription,
    [MaxLength(500)] string? ThumbnailUrl,
    decimal PriceInr,
    bool IsFirstTestFree,
    [MaxLength(20)] string Language = "English"
);

public record UpdateTestSeriesRequest(
    [Required, MaxLength(300)] string Title,
    [Required] int ExamTypeId,
    SeriesType SeriesType,
    [MaxLength(500)] string? ShortDescription,
    string? FullDescription,
    [MaxLength(500)] string? ThumbnailUrl,
    decimal PriceInr,
    bool IsFirstTestFree,
    [MaxLength(20)] string Language = "English"
);

/// <summary>Card view for list pages.</summary>
public record TestSeriesListDto(
    Guid     Id,
    string   Title,
    string   Slug,
    string   ExamTypeName,
    string   ProviderName,
    string?  ProviderLogoUrl,
    string   SeriesType,
    decimal  PriceInr,
    bool     IsFirstTestFree,
    string   Language,
    string   Status,
    int      TestCount,
    int      PurchaseCount,
    decimal  AvgRating,
    int      ReviewCount,
    string?  ThumbnailUrl,
    DateTime? PublishedAt
);

/// <summary>Full detail view for the test series page.</summary>
public record TestSeriesDetailDto(
    Guid     Id,
    string   Title,
    string   Slug,
    int      ExamTypeId,
    string   ExamTypeName,
    int      ProviderId,
    string   ProviderName,
    string?  ProviderLogoUrl,
    string?  ProviderBio,
    string   SeriesType,
    string?  ShortDescription,
    string?  FullDescription,
    string?  ThumbnailUrl,
    decimal  PriceInr,
    bool     IsFirstTestFree,
    string   Language,
    string   Status,
    int      TestCount,
    int      PurchaseCount,
    decimal  AvgRating,
    int      ReviewCount,
    DateTime? PublishedAt,
    IReadOnlyList<SeriesTestDto> Tests,
    IReadOnlyList<ReviewDto>     RecentReviews
);

public record SeriesTestDto(
    Guid   TestId,
    string TestTitle,
    int    SortOrder,
    bool   IsFreePreview,
    int    TotalQuestions,
    int    DurationMinutes
);

// ─────────────────────────────────────────────────────────────────────────────
// STOREFRONT (public-facing catalog)
// ─────────────────────────────────────────────────────────────────────────────

public record HomepageDto(
    IReadOnlyList<CmsBannerDto>     Banners,
    IReadOnlyList<ExamCategoryDto>  ExamCategories,
    IReadOnlyList<TestSeriesListDto> FreeTests,
    IReadOnlyList<TestSeriesListDto> TopSelling,
    IReadOnlyList<TestSeriesListDto> NewArrivals
);

public record CmsBannerDto(
    int     Id,
    string  Title,
    string? SubTitle,
    string? ImageUrl,
    string? LinkUrl,
    int     SortOrder
);

public record ExamCategoryDto(
    int    Id,
    string Name,
    string? Emoji,
    int    SeriesCount
);

public record TestSeriesSearchRequest(
    string?    Query,
    int?       ExamTypeId,
    SeriesType? SeriesType,
    decimal?   MinPrice,
    decimal?   MaxPrice,
    string?    Language,
    string     SortBy = "popular",   // "popular" | "newest" | "price_asc" | "price_desc" | "rating"
    int        Page   = 1,
    int        PageSize = 20
);

// ─────────────────────────────────────────────────────────────────────────────
// PROMO CODE
// ─────────────────────────────────────────────────────────────────────────────

public record PromoCodeValidateRequest(
    [Required, MaxLength(50)] string Code,
    Guid SeriesId,
    decimal OrderAmount
);

public record PromoCodeValidateResponse(
    bool     IsValid,
    string?  ErrorMessage,
    decimal  DiscountAmount,
    string   DiscountType,    // "Flat" | "Percentage"
    decimal  FinalAmount
);

// ─────────────────────────────────────────────────────────────────────────────
// ORDERS
// ─────────────────────────────────────────────────────────────────────────────

public record CreateOrderRequest(
    Guid    SeriesId,
    string? PromoCode
);

public record CreateOrderResponse(
    Guid    OrderId,
    string  BookingRef,
    string  RazorpayOrderId,
    string  RazorpayKeyId,
    decimal AmountInr,
    decimal GstAmount,
    decimal BookingFee,
    decimal DiscountApplied,
    decimal GrandTotal,
    string  Currency = "INR"
);

public record VerifyPaymentRequest(
    [Required] Guid   OrderId,
    [Required] string RazorpayOrderId,
    [Required] string RazorpayPaymentId,
    [Required] string RazorpaySignature
);

public record OrderDto(
    Guid        Id,
    string      BookingRef,
    Guid        SeriesId,
    string      SeriesTitle,
    string?     SeriesThumbnailUrl,
    decimal     AmountInr,
    decimal     GstAmount,
    decimal     BookingFee,
    decimal     DiscountApplied,
    decimal     GrandTotal,
    string?     PromoCodeApplied,
    string      Status,        // "Pending" | "Paid" | "Failed" | "Refunded"
    DateTime    CreatedAt
);

// ─────────────────────────────────────────────────────────────────────────────
// ENTITLEMENT
// ─────────────────────────────────────────────────────────────────────────────

public record EntitlementCheckResponse(
    bool      HasAccess,
    Guid?     EntitlementId,
    DateTime? ExpiresAt,         // null = lifetime
    bool      IsExpired
);

// ─────────────────────────────────────────────────────────────────────────────
// STUDENT EXAM / TEST ENGINE
// ─────────────────────────────────────────────────────────────────────────────

public record ExamConfigDto(
    Guid                      TestId,
    string                    TestTitle,
    int                       DurationMinutes,
    IReadOnlyList<ExamSectionDto> Sections
);

public record ExamSectionDto(
    string                        SectionName,
    int                           TotalQuestions,
    IReadOnlyList<ExamQuestionDto> Questions
);

public record ExamQuestionDto(
    Guid                          QuestionId,
    int                           QuestionNumber,
    string                        QuestionText,
    string                        QuestionType,   // "MCQ_Single" | "MCQ_Multi" | "Numerical"
    IReadOnlyList<ExamOptionDto>   Options,
    decimal                       Marks,
    decimal                       NegativeMarks
);

public record ExamOptionDto(
    Guid   OptionId,
    string OptionText,
    char   Label        // A, B, C, D
);

public record SubmitExamRequest(
    Guid                            TestId,
    IReadOnlyList<StudentAnswerDto> Answers,
    int                             TotalTimeTakenSeconds
);

public record StudentAnswerDto(
    Guid          QuestionId,
    List<Guid>?   SelectedOptionIds,  // null for numerical
    decimal?      NumericalAnswer,
    bool          MarkedForReview,
    int           TimeSpentSeconds
);

public record ExamResultDto(
    Guid    ResultId,
    Guid    TestId,
    string  TestTitle,
    int     TotalQuestions,
    int     AttemptedCount,
    int     CorrectCount,
    int     WrongCount,
    int     SkippedCount,
    decimal ScoreObtained,
    decimal MaxScore,
    decimal Percentile,
    int?    Rank,
    int?    TotalAttempts,
    int     TimeTakenSeconds,
    int     DurationSeconds,
    IReadOnlyList<SectionResultDto>   SectionBreakdown,
    IReadOnlyList<AnswerReviewDto>    AnswerKey
);

public record SectionResultDto(
    string  SectionName,
    int     Attempted,
    int     Correct,
    int     Wrong,
    decimal Score,
    decimal MaxScore
);

public record AnswerReviewDto(
    Guid          QuestionId,
    int           QuestionNumber,
    string        QuestionText,
    string        QuestionType,
    IReadOnlyList<ExamOptionDto> Options,
    List<Guid>?   SelectedOptionIds,
    decimal?      NumericalAnswer,
    List<Guid>?   CorrectOptionIds,
    decimal?      CorrectNumericalAnswer,
    bool          IsCorrect,
    decimal       MarksAwarded,
    string?       Solution
);

// ─────────────────────────────────────────────────────────────────────────────
// STUDENT DASHBOARD
// ─────────────────────────────────────────────────────────────────────────────

public record StudentDashboardDto(
    int     TotalPurchases,
    int     TestsAttempted,
    decimal AvgScore,
    IReadOnlyList<PurchasedSeriesDto> PurchasedSeries,
    IReadOnlyList<OrderDto>           RecentOrders
);

public record PurchasedSeriesDto(
    Guid      SeriesId,
    string    SeriesTitle,
    string?   SeriesThumbnailUrl,
    string    ExamTypeName,
    string    ProviderName,
    int       TotalTests,
    int       AttempedTests,
    DateTime  EntitledAt,
    DateTime? ExpiresAt
);

// ─────────────────────────────────────────────────────────────────────────────
// REVIEWS
// ─────────────────────────────────────────────────────────────────────────────

public record SubmitReviewRequest(
    Guid    SeriesId,
    [Range(1, 5)] int Rating,
    [MaxLength(1000)] string? Comment
);

public record ReviewDto(
    Guid     Id,
    Guid     StudentId,
    string   StudentName,
    int      Rating,
    string?  Comment,
    DateTime CreatedAt
);

// ─────────────────────────────────────────────────────────────────────────────
// ADMIN — MARKETPLACE MANAGEMENT
// ─────────────────────────────────────────────────────────────────────────────

public record MarketplaceDashboardDto(
    int     TotalProviders,
    int     PendingProviders,
    int     TotalSeries,
    int     PendingReviewSeries,
    int     PublishedSeries,
    int     TotalOrders,
    decimal GmvAllTime,         // Gross Merchandise Value
    decimal GmvThisMonth,
    decimal PlatformRevenueAllTime,
    decimal PlatformRevenueThisMonth,
    decimal PendingPayoutsAmount,
    IReadOnlyList<DailyGmvDto>  Last30DaysGmv,
    IReadOnlyList<TopSeriesDto> TopSeries
);

public record DailyGmvDto(
    DateOnly Date,
    decimal  Gmv,
    int      Orders
);

public record TopSeriesDto(
    Guid    SeriesId,
    string  SeriesTitle,
    string  ProviderName,
    int     PurchaseCount,
    decimal Revenue
);

public record TestReviewQueueDto(
    Guid     SeriesId,
    string   SeriesTitle,
    string   ProviderName,
    string   ExamTypeName,
    string   SeriesType,
    decimal  PriceInr,
    DateTime SubmittedAt
);

public record ReviewDecisionRequest(
    [Required] bool   Approved,
    [MaxLength(1000)] string? Notes
);

public record AdminProviderListDto(
    int      Id,
    Guid     UserId,
    string   UserFullName,
    string   UserEmail,
    string   InstituteName,
    string   Status,
    int      PublishedSeries,
    int      TotalSales,
    DateTime CreatedAt
);

// ─────────────────────────────────────────────────────────────────────────────
// COMMISSIONS & PAYOUTS
// ─────────────────────────────────────────────────────────────────────────────

public record CommissionDto(
    Guid             Id,
    Guid             OrderId,
    string           BookingRef,
    string           SeriesTitle,
    decimal          GrossAmount,
    decimal          PlatformPct,
    decimal          PlatformAmount,
    decimal          ProviderPct,
    decimal          ProviderAmount,
    CommissionStatus Status,
    Guid?            PayoutId,
    DateTime         CreatedAt
);

public record InitiatePayoutRequest(
    [Required] int ProviderId,
    string? Notes
);

public record PayoutDto(
    Guid          Id,
    int           ProviderId,
    string        ProviderName,
    decimal       TotalAmount,
    int           CommissionCount,
    PayoutStatus  Status,
    string?       TransactionRef,
    string?       Notes,
    DateTime      InitiatedAt,
    DateTime?     CompletedAt
);

// ─────────────────────────────────────────────────────────────────────────────
// CMS BANNERS (Admin)
// ─────────────────────────────────────────────────────────────────────────────

public record CreateBannerRequest(
    [Required, MaxLength(200)] string Title,
    [MaxLength(500)] string? SubTitle,
    [MaxLength(500)] string? ImageUrl,
    [MaxLength(500)] string? LinkUrl,
    int SortOrder = 0,
    DateTime? ValidFrom = null,
    DateTime? ValidTo   = null
);

public record UpdateBannerRequest(
    [Required, MaxLength(200)] string Title,
    [MaxLength(500)] string? SubTitle,
    [MaxLength(500)] string? ImageUrl,
    [MaxLength(500)] string? LinkUrl,
    int  SortOrder = 0,
    bool IsActive  = true,
    DateTime? ValidFrom = null,
    DateTime? ValidTo   = null
);

// ─────────────────────────────────────────────────────────────────────────────
// PROMO CODES (Admin)
// ─────────────────────────────────────────────────────────────────────────────

public record CreatePromoCodeRequest(
    [Required, MaxLength(50)] string Code,
    DiscountType  DiscountType,
    decimal       DiscountValue,
    decimal?      MinOrderAmount,
    decimal?      MaxDiscount,
    Guid?         SeriesId,
    int?          UsageLimit,
    DateTime?     ValidFrom,
    DateTime?     ValidTo
);

public record PromoCodeDto(
    int          Id,
    string       Code,
    string       DiscountType,
    decimal      DiscountValue,
    decimal?     MinOrderAmount,
    decimal?     MaxDiscount,
    Guid?        SeriesId,
    int?         UsageLimit,
    int          UsedCount,
    bool         IsActive,
    DateTime?    ValidFrom,
    DateTime?    ValidTo,
    DateTime     CreatedAt
);
