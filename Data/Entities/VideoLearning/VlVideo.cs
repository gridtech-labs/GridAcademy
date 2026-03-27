using System.ComponentModel.DataAnnotations;

namespace GridAcademy.Data.Entities.VideoLearning;

public class VlVideo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int DomainId { get; set; }
    public int CategoryId { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = "";

    public string? Description { get; set; }

    [MaxLength(500)]
    public string? FilePath { get; set; }

    [MaxLength(500)]
    public string? ThumbnailPath { get; set; }

    public int DurationSeconds { get; set; } = 0;
    public bool IsFreePreview { get; set; } = false;
    public VideoStatus Status { get; set; } = VideoStatus.Draft;
    public int SortOrder { get; set; } = 0;
    public long FileSizeBytes { get; set; } = 0;

    [MaxLength(255)]
    public string? OriginalFileName { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public VlDomain Domain { get; set; } = null!;
    public VlVideoCategory Category { get; set; } = null!;
    public ICollection<VlVideoProgress> Progresses { get; set; } = [];
}
