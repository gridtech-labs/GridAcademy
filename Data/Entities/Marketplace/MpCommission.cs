namespace GridAcademy.Data.Entities.Marketplace;

/// <summary>Commission record for each paid order — split between provider and platform.</summary>
public class MpCommission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }
    public int ProviderId { get; set; }

    public decimal GrossAmount { get; set; }

    public decimal PlatformPct { get; set; } = 30;
    public decimal PlatformAmount { get; set; }

    public decimal ProviderPct { get; set; } = 70;
    public decimal ProviderAmount { get; set; }

    public CommissionStatus Status { get; set; } = CommissionStatus.Pending;

    public Guid? PayoutId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public MpOrder Order { get; set; } = null!;
    public MpProvider Provider { get; set; } = null!;
    public MpPayout? Payout { get; set; }
}
