using GridAcademy.Data.Entities;

namespace GridAcademy.Data.Entities.Marketplace;

/// <summary>Grants a student access to all tests in a series.</summary>
public class MpEntitlement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StudentId { get; set; }
    public Guid SeriesId { get; set; }
    public Guid OrderId { get; set; }

    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Null = lifetime access.</summary>
    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public User Student { get; set; } = null!;
    public MpTestSeries Series { get; set; } = null!;
    public MpOrder Order { get; set; } = null!;
}
