using System.ComponentModel.DataAnnotations;

namespace GridAcademy.Data.Entities.VideoLearning;

public class VlDomain
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<VlVideoCategory> VideoCategories { get; set; } = [];
    public ICollection<VlVideo>          Videos           { get; set; } = [];
    public ICollection<VlLearningPath>   LearningPaths    { get; set; } = [];
    public ICollection<VlProgram>        Programs         { get; set; } = [];
}
