namespace GridAcademy.Data.Entities.VideoLearning;

public class VlVideoProgress
{
    public Guid Id           { get; set; } = Guid.NewGuid();
    public Guid EnrollmentId { get; set; }
    public Guid VideoId      { get; set; }

    public VideoProgressStatus Status         { get; set; } = VideoProgressStatus.NotStarted;
    public int                  WatchedSeconds { get; set; } = 0;

    public DateTime? CompletedAt    { get; set; }
    public DateTime  LastUpdatedAt  { get; set; }

    public VlEnrollment Enrollment { get; set; } = null!;
    public VlVideo      Video      { get; set; } = null!;
}
