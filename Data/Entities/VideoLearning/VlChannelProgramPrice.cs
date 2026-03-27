using System.ComponentModel.DataAnnotations.Schema;

namespace GridAcademy.Data.Entities.VideoLearning;

public class VlChannelProgramPrice
{
    public int Id            { get; set; }
    public int ChannelId     { get; set; }
    public int PricingPlanId { get; set; }

    [Column(TypeName = "numeric(12,2)")]
    public decimal? OverridePriceInr { get; set; }

    [Column(TypeName = "numeric(12,2)")]
    public decimal? OverridePriceUsd { get; set; }

    public bool IsActive { get; set; } = true;

    public VlSalesChannel      Channel    { get; set; } = null!;
    public VlProgramPricingPlan PricingPlan { get; set; } = null!;
}
