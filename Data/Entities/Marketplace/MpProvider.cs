using System.ComponentModel.DataAnnotations;
using GridAcademy.Data.Entities;

namespace GridAcademy.Data.Entities.Marketplace;

/// <summary>Test content provider (coaching institute or individual educator).</summary>
public class MpProvider
{
    public int Id { get; set; }

    /// <summary>Linked platform user (role = "Provider").</summary>
    public Guid UserId { get; set; }

    [Required, MaxLength(200)]
    public string InstituteName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PanNumber { get; set; }

    /// <summary>Bank account number — stored AES-256 encrypted.</summary>
    [MaxLength(500)]
    public string? BankAccountEncrypted { get; set; }

    [MaxLength(11)]
    public string? IfscCode { get; set; }

    [MaxLength(200)]
    public string? AccountHolderName { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }

    [MaxLength(300)]
    public string? LogoUrl { get; set; }

    public ProviderStatus Status { get; set; } = ProviderStatus.Pending;

    [MaxLength(1000)]
    public string? AdminNotes { get; set; }

    public bool AgreedToTerms { get; set; } = false;
    public DateTime? AgreedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<MpTestSeries> TestSeries { get; set; } = [];
    public ICollection<MpCommission> Commissions { get; set; } = [];
    public ICollection<MpPayout> Payouts { get; set; } = [];
}
