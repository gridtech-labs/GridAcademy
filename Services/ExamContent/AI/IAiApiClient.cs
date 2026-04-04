namespace GridAcademy.Services.ExamContent.AI;

public interface IAiApiClient
{
    Task<(string Html, AiTokenUsage? Usage)> GenerateHtmlAsync(string prompt, CancellationToken ct = default);
}
