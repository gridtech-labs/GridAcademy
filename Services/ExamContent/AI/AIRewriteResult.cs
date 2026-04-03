namespace GridAcademy.Services.ExamContent.AI;

public record AIRewriteResult(string ContentHtml, string MetaTitle, string MetaDescription);

public record AiTokenUsage(int? PromptTokens, int? CompletionTokens, int? TotalTokens);
