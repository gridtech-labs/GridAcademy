namespace GridAcademy.Services.ExamContent;

public interface IContentProcessingService
{
    string CleanHtml(string htmlContent);
    string ExtractSummary(string htmlContent, int wordLimit = 150);
    string GenerateSlug(string input);
    string GenerateContentHash(string htmlContent);
    string NormalizeForHash(string htmlContent);
}
