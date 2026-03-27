using System.ComponentModel.DataAnnotations;

namespace GridAcademy.Data.Entities.VideoLearning;

/// <summary>
/// A single node in a Learning Path tree.
///
/// Node types (NodeType field):
///   'N'  = Module  (named container; ContentId is null)
///   'AS' = Assessment / Test
///   'VL' = Video
///   'SC' = SCORM
///   'PD' = PDF
///   'HT' = HTML
///
/// Hierarchy:
///   ParentNodeId = null  → top-level node (module or direct content)
///   ParentNodeId = N     → child of module N
/// </summary>
public class VlLearningPathNode
{
    public int Id { get; set; }
    public Guid LearningPathId { get; set; }
    public int? ParentNodeId { get; set; }      // null = top-level

    /// <summary>Two-char type code: N | AS | VL | SC | PD | HT</summary>
    [Required, MaxLength(2)]
    public string NodeType { get; set; } = LpNodeType.Module;

    [Required, MaxLength(500)]
    public string Title { get; set; } = "";

    /// <summary>FK to the source content record (VlVideo.Id / Test.Id / VlContentFile.Id).
    /// Null for module nodes.</summary>
    public Guid? ContentId { get; set; }

    public bool IsPreview { get; set; } = false;
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public VlLearningPath LearningPath { get; set; } = null!;
    public VlLearningPathNode? ParentNode { get; set; }
    public ICollection<VlLearningPathNode> ChildNodes { get; set; } = [];
}

/// <summary>Node type constants.</summary>
public static class LpNodeType
{
    public const string Module     = "N";
    public const string Assessment = "AS";
    public const string Video      = "VL";
    public const string Scorm      = "SC";
    public const string Pdf        = "PD";
    public const string Html       = "HT";

    public static string Label(string nodeType) => nodeType switch
    {
        Module     => "Module",
        Assessment => "Assessment",
        Video      => "Video",
        Scorm      => "SCORM",
        Pdf        => "PDF",
        Html       => "HTML",
        _          => nodeType
    };

    public static string BadgeClass(string nodeType) => nodeType switch
    {
        Module     => "bg-secondary",
        Assessment => "bg-warning text-dark",
        Video      => "bg-primary",
        Scorm      => "bg-info text-dark",
        Pdf        => "bg-danger",
        Html       => "bg-success",
        _          => "bg-light text-dark"
    };

    public static bool IsContentType(string nodeType) => nodeType != Module;
}
