using System.ComponentModel.DataAnnotations;
using GridAcademy.Data.Entities;

namespace GridAcademy.Data.Entities.Marketplace;

/// <summary>Purchase order for a test series.</summary>
public class MpOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StudentId { get; set; }
    public Guid SeriesId { get; set; }

    public decimal AmountInr { get; set; }
    public decimal GstAmount { get; set; }
    public decimal BookingFee { get; set; }
    public decimal GrandTotal { get; set; }

    [MaxLength(50)]
    public string? PromoCodeApplied { get; set; }

    public decimal DiscountApplied { get; set; } = 0;

    /// <summary>Razorpay order_id.</summary>
    [MaxLength(100)]
    public string? RazorpayOrderId { get; set; }

    /// <summary>Razorpay payment_id (set after successful payment).</summary>
    [MaxLength(100)]
    public string? RazorpayPaymentId { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>Short human-readable booking reference (e.g. GA-20260325-XXXX).</summary>
    [Required, MaxLength(30)]
    public string BookingRef { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User Student { get; set; } = null!;
    public MpTestSeries Series { get; set; } = null!;
    public MpEntitlement? Entitlement { get; set; }
    public MpCommission? Commission { get; set; }
}
