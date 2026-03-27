using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GridAcademy.Data.Entities;

namespace GridAcademy.Data.Entities.VideoLearning;

public class VlEnrollment
{
    public Guid Id            { get; set; } = Guid.NewGuid();
    public Guid UserId        { get; set; }
    public Guid ProgramId     { get; set; }
    public int  PricingPlanId { get; set; }

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;

    [Column(TypeName = "numeric(12,2)")]
    public decimal AmountPaidInr { get; set; }

    [Column(TypeName = "numeric(12,2)")]
    public decimal AmountPaidUsd { get; set; }

    [MaxLength(50)]
    public string? CouponCode { get; set; }

    [Column(TypeName = "numeric(12,2)")]
    public decimal? DiscountApplied { get; set; }

    public int? ChannelId { get; set; }

    public DateTime  EnrolledAt { get; set; }
    public DateTime? ExpiresAt  { get; set; }
    public DateTime  UpdatedAt  { get; set; }

    public User                User        { get; set; } = null!;
    public VlProgram           Program     { get; set; } = null!;
    public VlProgramPricingPlan PricingPlan { get; set; } = null!;
    public VlSalesChannel?     Channel     { get; set; }
    public ICollection<VlVideoProgress> VideoProgresses { get; set; } = [];
}
