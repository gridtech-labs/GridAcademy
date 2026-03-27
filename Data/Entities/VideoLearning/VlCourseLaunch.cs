using System.ComponentModel.DataAnnotations;

namespace GridAcademy.Data.Entities.VideoLearning;

public class VlCourseLaunch
{
    public int  Id        { get; set; }
    public Guid ProgramId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    public CourseLaunchStatus Status { get; set; } = CourseLaunchStatus.Active;

    [MaxLength(500)]
    public string? BlockedReason { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate   { get; set; }

    public int MaxEnrollments { get; set; } = 0;  // 0 = unlimited

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public VlProgram Program { get; set; } = null!;
}
