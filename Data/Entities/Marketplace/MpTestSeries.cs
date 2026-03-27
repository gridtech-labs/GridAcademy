using System.ComponentModel.DataAnnotations;
using GridAcademy.Data.Entities.Content;

namespace GridAcademy.Data.Entities.Marketplace;

/// <summary>A test series published on the marketplace by a provider.</summary>
public class MpTestSeries
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int ProviderId { get; set; }

    /// <summary>FK to ExamType master (SSC CGL, IBPS PO, etc.).</summary>
    public int ExamTypeId { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    /// <summary>URL-friendly slug — must be unique among Published series.</summary>
    [Required, MaxLength(350)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    public string? FullDescription { get; set; }

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    public SeriesType SeriesType { get; set; } = SeriesType.FullMock;

    /// <summary>Base price in INR paise (0 = free).</summary>
    public decimal PriceInr { get; set; } = 0;

    public bool IsFirstTestFree { get; set; } = false;

    [MaxLength(20)]
    public string Language { get; set; } = "English";

    public SeriesStatus Status { get; set; } = SeriesStatus.Draft;

    [MaxLength(1000)]
    public string? ReviewNotes { get; set; }

    public DateTime? PublishedAt { get; set; }

    public int PurchaseCount { get; set; } = 0;
    public decimal AvgRating { get; set; } = 0;
    public int ReviewCount { get; set; } = 0;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public MpProvider Provider { get; set; } = null!;
    public ExamType ExamType { get; set; } = null!;
    public ICollection<MpSeriesTest> SeriesTests { get; set; } = [];
    public ICollection<MpOrder> Orders { get; set; } = [];
    public ICollection<MpEntitlement> Entitlements { get; set; } = [];
    public ICollection<MpReview> Reviews { get; set; } = [];
}
