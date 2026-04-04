namespace GridAcademy.Services.ExamContent.Options;

public class AiRewriteOptions
{
    public const string SectionName = "AIRewrite";

    public string Provider { get; set; } = "OpenAI";
    public string BaseUrl { get; set; } = "https://api.openai.com";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4.1-mini";
    public int TimeoutSeconds { get; set; } = 120;
    public int DefaultBatchSize { get; set; } = 5;
    public int MaxBatchSize { get; set; } = 20;
    public int MaxRetries { get; set; } = 2;
    public double Temperature { get; set; } = 0.3;
}
