using System.ComponentModel.DataAnnotations;

namespace GridAcademy.Data.Entities.VideoLearning;

public class VlLearningPath
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int DomainId { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = "";

    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ThumbnailPath { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public VlDomain Domain { get; set; } = null!;

    // All nodes (modules + content) — hierarchical via VlLearningPathNode.ParentNodeId
    public ICollection<VlLearningPathNode> Nodes { get; set; } = [];

    // Programs that include this learning path
    public ICollection<VlProgramLearningPath> ProgramLearningPaths { get; set; } = [];
}
