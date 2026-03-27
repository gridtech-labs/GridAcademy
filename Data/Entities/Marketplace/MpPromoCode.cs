using System.ComponentModel.DataAnnotations;

namespace GridAcademy.Data.Entities.Marketplace;

/// <summary>Promotional discount code applicable at checkout.</summary>
public class MpPromoCode
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;  // stored UPPERCASE

    public DiscountType DiscountType { get; set; } = DiscountType.Flat;

    public decimal DiscountValue { get; set; }

    public decimal? MinOrderAmount { get; set; }

    public decimal? MaxDiscount { get; set; }

    /// <summary>If set, only applies to this series.</summary>
    public Guid? SeriesId { get; set; }

    public int? UsageLimit { get; set; }

    public int UsedCount { get; set; } = 0;

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
