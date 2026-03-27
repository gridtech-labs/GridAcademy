using GridAcademy.Data.Entities.Assessment;

namespace GridAcademy.Data.Entities.Marketplace;

/// <summary>Links a test (from the assessment module) to a marketplace test series.</summary>
public class MpSeriesTest
{
    public int Id { get; set; }
    public Guid SeriesId { get; set; }

    /// <summary>FK to existing Test entity in the assessment module.</summary>
    public Guid TestId { get; set; }

    public int SortOrder { get; set; } = 0;

    /// <summary>If true, this test can be attempted without purchasing the series.</summary>
    public bool IsFreePreview { get; set; } = false;

    // Navigation
    public MpTestSeries Series { get; set; } = null!;
    public Test Test { get; set; } = null!;
}
