using System.ComponentModel.DataAnnotations;

namespace GridAcademy.Data.Entities.VideoLearning;

public class VlProgram
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int DomainId { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = "";

    [MaxLength(50)]
    public string? LearningCode { get; set; }

    public bool IsBlendedLearning { get; set; } = false;

    public string? ShortDescription { get; set; }
    public string? Description { get; set; }  // HTML rich text

    [MaxLength(500)]
    public string? ThumbnailPath { get; set; }

    public ProgramStatus Status { get; set; } = ProgramStatus.Draft;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public VlDomain Domain { get; set; } = null!;
    public ICollection<VlProgramPricingPlan>   PricingPlans         { get; set; } = [];
    public ICollection<VlProgramLearningPath>  ProgramLearningPaths { get; set; } = [];
    public ICollection<VlEnrollment>           Enrollments          { get; set; } = [];
    public ICollection<VlCoupon>               Coupons              { get; set; } = [];
    public ICollection<VlCourseLaunch>         CourseLaunches       { get; set; } = [];
}
