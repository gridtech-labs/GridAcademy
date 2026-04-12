using System.ComponentModel.DataAnnotations;

namespace GridAcademy.Data.Entities.Payment;

/// <summary>Immutable audit log of every payment event for an ExamOrder.</summary>
public class ExamOrderTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ExamOrderId { get; set; }

    /// <summary>e.g. "initiated", "razorpay.order.created", "payment.captured", "payment.failed", "refunded", "webhook.received"</summary>
    [Required, MaxLength(80)]
    public string Event { get; set; } = string.Empty;

    [MaxLength(100)] public string? RazorpayPaymentId { get; set; }
    [MaxLength(100)] public string? RazorpayOrderId   { get; set; }

    public decimal? Amount { get; set; }

    /// <summary>Raw JSON payload (webhook body or Razorpay response).</summary>
    public string? RawPayload { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ExamOrder Order { get; set; } = null!;
}
