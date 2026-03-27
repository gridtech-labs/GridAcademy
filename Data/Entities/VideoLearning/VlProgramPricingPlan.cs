using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GridAcademy.Data.Entities.VideoLearning;

public class VlProgramPricingPlan
{
    public int  Id        { get; set; }
    public Guid ProgramId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";  // e.g. "Monthly", "Yearly", "Lifetime"

    [Column(TypeName = "numeric(12,2)")]
    public decimal PriceInr { get; set; }

    [Column(TypeName = "numeric(12,2)")]
    public decimal PriceUsd { get; set; }

    [Column(TypeName = "numeric(12,2)")]
    public decimal? OriginalPriceInr { get; set; }

    [Column(TypeName = "numeric(12,2)")]
    public decimal? OriginalPriceUsd { get; set; }

    public int? ValidityDays { get; set; }  // null = Lifetime

    public bool IsActive  { get; set; } = true;
    public int  SortOrder { get; set; } = 0;

    public VlProgram Program { get; set; } = null!;
    public ICollection<VlChannelProgramPrice> ChannelPrices { get; set; } = [];
    public ICollection<VlEnrollment>          Enrollments   { get; set; } = [];
}
