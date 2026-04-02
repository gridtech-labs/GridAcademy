namespace GridAcademy.Services.ExamContent.Models;

public enum ContentProcessingStatus
{
    Saved = 1,
    SkippedDuplicate = 2,
    Error = 3
}

public sealed class ContentProcessingResult
{
    public ContentProcessingStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Slug { get; init; }

    public static ContentProcessingResult Saved(string slug) => new()
    {
        Status = ContentProcessingStatus.Saved,
        Slug = slug,
        Message = "Saved as draft."
    };

    public static ContentProcessingResult Duplicate(string hash) => new()
    {
        Status = ContentProcessingStatus.SkippedDuplicate,
        Message = $"Duplicate skipped for hash {hash}."
    };

    public static ContentProcessingResult Failed(string message) => new()
    {
        Status = ContentProcessingStatus.Error,
        Message = message
    };
}
