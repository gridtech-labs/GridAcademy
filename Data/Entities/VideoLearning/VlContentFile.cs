using System.ComponentModel.DataAnnotations;

namespace GridAcademy.Data.Entities.VideoLearning;

/// <summary>
/// Stores non-video content files: SCORM packages, HTML pages, PDF documents.
/// </summary>
public class VlContentFile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int DomainId { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = "";

    public string? Description { get; set; }
    public ContentFileType ContentType { get; set; }

    [MaxLength(500)]
    public string? FilePath { get; set; }

    [MaxLength(255)]
    public string? OriginalFileName { get; set; }

    public long FileSizeBytes { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public VlDomain Domain { get; set; } = null!;
}
