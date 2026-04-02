using System.ComponentModel.DataAnnotations;

namespace GridAcademy.Data.Entities.Exam;

public enum ExamNotificationType
{
    Notification = 1,
    AdmitCard = 2,
    Result = 3,
    Syllabus = 4
}

public enum PublicationStatus
{
    Draft = 0,
    Published = 1
}

public class Exam
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(220)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(100)]
    public string? Level { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ExamNotification> Notifications { get; set; } = [];
}

public class ExamNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExamId { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(320)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public string ContentHtml { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Summary { get; set; }

    public ExamNotificationType NotificationType { get; set; } = ExamNotificationType.Notification;

    public string? ImportantDates { get; set; }

    [Required, MaxLength(500)]
    public string SourceUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? CanonicalUrl { get; set; }

    [MaxLength(300)]
    public string? MetaTitle { get; set; }

    [MaxLength(500)]
    public string? MetaDescription { get; set; }

    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public PublicationStatus Status { get; set; } = PublicationStatus.Draft;

    public Exam? Exam { get; set; }
    public ICollection<ContentVersion> Versions { get; set; } = [];
}

public class ContentVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(80)]
    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    [Required]
    public string ContentHtml { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ContentHash
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(64)]
    public string HashValue { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string SourceUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
