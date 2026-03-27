using System.ComponentModel.DataAnnotations;

namespace GridAcademy.Data.Entities.Marketplace;

/// <summary>Bulk payout to a provider (aggregates multiple commission records).</summary>
public class MpPayout
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int ProviderId { get; set; }
    public decimal Amount { get; set; }

    [MaxLength(200)]
    public string? RazorpayTransferId { get; set; }

    public PayoutStatus Status { get; set; } = PayoutStatus.Initiated;

    [MaxLength(500)]
    public string? FailureReason { get; set; }

    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public MpProvider Provider { get; set; } = null!;
    public ICollection<MpCommission> Commissions { get; set; } = [];
}
