namespace GridAcademy.Modules.AiGeneration.Domain.Entities;

/// <summary>
/// Daily API usage tracker. One row per (date, provider, model).
/// Used to enforce the free-tier quota safety margin.
/// </summary>
public class LlmUsage
{
    public int    Id             { get; set; }
    public DateOnly Date         { get; set; }
    public string Provider       { get; set; } = "gemini";
    public string Model          { get; set; } = "";
    public int    PromptTokens   { get; set; } = 0;
    public int    CompletionTokens { get; set; } = 0;
    public int    TotalTokens    { get; set; } = 0;
    public int    RequestCount   { get; set; } = 0;
    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt    { get; set; } = DateTime.UtcNow;
}
