using GridAcademy.Data.Entities.Payment;

namespace GridAcademy.DTOs.Payment;

// ── Create Order ─────────────────────────────────────────────────────────────
public record CreateExamOrderRequest(Guid ExamPageId, string? OfferCode);

public record CreateExamOrderResponse(
    Guid   OrderId,
    string BookingRef,
    string RazorpayOrderId,
    string RazorpayKeyId,          // public key to send to frontend
    decimal OriginalAmount,
    decimal DiscountAmount,
    decimal GstAmount,
    decimal GrandTotal,
    string? OfferTitle,
    string  ExamTitle,
    string  ExamSlug,
    // Razorpay prefill
    string? PrefillName,
    string? PrefillEmail
);

// ── Verify Payment ────────────────────────────────────────────────────────────
public record VerifyExamPaymentRequest(
    Guid   OrderId,
    string RazorpayOrderId,
    string RazorpayPaymentId,
    string RazorpaySignature
);

// ── Offer validation ──────────────────────────────────────────────────────────
public record ValidateOfferRequest(string Code, Guid ExamPageId, decimal OriginalAmount);

public record ValidateOfferResponse(
    bool    IsValid,
    string? Message,
    string? OfferTitle,
    decimal DiscountAmount,
    decimal FinalAmount,
    int?    OfferId
);

// ── Access check ──────────────────────────────────────────────────────────────
public record ExamAccessResponse(bool HasAccess, DateTime? ExpiresAt);

// ── Order list (admin) ────────────────────────────────────────────────────────
public record ExamOrderListItem(
    Guid   Id,
    string BookingRef,
    string StudentName,
    string StudentEmail,
    string ExamTitle,
    decimal GrandTotal,
    ExamOrderStatus Status,
    DateTime CreatedAt,
    DateTime? PaidAt,
    string? RazorpayPaymentId
);

// ── Order detail (admin) ──────────────────────────────────────────────────────
public record ExamOrderDetail(
    Guid   Id,
    string BookingRef,
    string StudentName,
    string StudentEmail,
    string ExamTitle,
    Guid   ExamPageId,
    decimal OriginalAmount,
    decimal DiscountAmount,
    decimal GstAmount,
    decimal GrandTotal,
    string? OfferCode,
    ExamOrderStatus Status,
    string? RazorpayOrderId,
    string? RazorpayPaymentId,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime? PaidAt,
    List<ExamOrderTransactionDto> Transactions
);

public record ExamOrderTransactionDto(
    Guid    Id,
    string  Event,
    string? RazorpayPaymentId,
    decimal? Amount,
    DateTime CreatedAt
);

// ── Offer DTO (admin) ─────────────────────────────────────────────────────────
public record ExamOfferDto(
    int    Id,
    string Code,
    string Title,
    string? Description,
    ExamOfferType OfferType,
    decimal Value,
    decimal MinOrderAmount,
    decimal? MaxDiscountAmount,
    Guid?   ExamPageId,
    string? ExamPageTitle,
    int?    MaxUses,
    int     UsedCount,
    bool    IsActive,
    DateTime? ValidFrom,
    DateTime? ValidTo
);

public record SaveExamOfferRequest(
    string Code,
    string Title,
    string? Description,
    ExamOfferType OfferType,
    decimal Value,
    decimal MinOrderAmount,
    decimal? MaxDiscountAmount,
    Guid?   ExamPageId,
    int?    MaxUses,
    bool    IsActive,
    DateTime? ValidFrom,
    DateTime? ValidTo
);
