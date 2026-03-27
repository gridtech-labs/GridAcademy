using System.ComponentModel.DataAnnotations;
using GridAcademy.Data.Entities;

namespace GridAcademy.Data.Entities.Marketplace;

/// <summary>Student review for a test series (submitted after completing at least one test).</summary>
public class MpReview
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StudentId { get; set; }
    public Guid SeriesId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public bool IsVisible { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User Student { get; set; } = null!;
    public MpTestSeries Series { get; set; } = null!;
}
