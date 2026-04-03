namespace GridAcademy.Services.ExamContent.AI;

public interface IAIRewriteService
{
    Task<AIRewriteResult> RewriteAsync(string rawHtml, string title, CancellationToken ct = default);
}
