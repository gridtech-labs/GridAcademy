namespace GridAcademy.Modules.AiGeneration.Infrastructure.Llm;

/// <summary>
/// Abstraction over any LLM API (Gemini, Anthropic, OpenAI…).
/// Swap providers by changing "Ai:LlmProvider" in appsettings without touching services.
/// </summary>
public interface ILLMProvider
{
    /// <summary>Name shown in llm_usage rows, e.g. "gemini" | "anthropic".</summary>
    string ProviderName { get; }

    /// <summary>The model identifier currently active for generation.</summary>
    string ModelName { get; }

    /// <summary>Send a prompt and get back a plain-text completion + token counts.</summary>
    Task<LlmCompletion> CompleteAsync(string prompt, CancellationToken ct = default);
}
