using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GridAcademy.Data.Entities.VideoLearning;

public class VlCoupon
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = "";  // stored UPPERCASE

    [MaxLength(300)]
    public string? Description { get; set; }

    public DiscountType DiscountType  { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    public decimal DiscountValue { get; set; }

    [Column(TypeName = "numeric(12,2)")]
    public decimal? MaxDiscountInr { get; set; }

    [Column(TypeName = "numeric(12,2)")]
    public decimal? MaxDiscountUsd { get; set; }

    public DateTime? ValidFrom   { get; set; }
    public DateTime? ValidTo     { get; set; }
    public int?      UsageLimit  { get; set; }  // null = unlimited
    public int       UsedCount   { get; set; } = 0;

    public Guid? ProgramId { get; set; }  // null = global coupon
    public bool  IsActive  { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public VlProgram? Program { get; set; }
}
