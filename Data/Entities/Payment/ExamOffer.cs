using System.ComponentModel.DataAnnotations;

namespace GridAcademy.Data.Entities.Payment;

/// <summary>Discount offer / promo code for exam page purchases.</summary>
public class ExamOffer
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;   // stored UPPERCASE, unique

    [Required, MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public ExamOfferType OfferType { get; set; } = ExamOfferType.PercentageDiscount;

    /// <summary>Percentage (0-100) for Percentage type, or fixed INR amount for Fixed type. Ignored for FreeAccess.</summary>
    public decimal Value { get; set; }

    /// <summary>Minimum order amount (before discount) required to use this offer.</summary>
    public decimal MinOrderAmount { get; set; } = 0;

    /// <summary>Maximum discount cap in INR (only for Percentage type). null = no cap.</summary>
    public decimal? MaxDiscountAmount { get; set; }

    /// <summary>null = global (all exam pages), set = only for specific exam.</summary>
    public Guid? ExamPageId { get; set; }

    /// <summary>null = unlimited uses.</summary>
    public int? MaxUses { get; set; }
    public int  UsedCount { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo   { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
