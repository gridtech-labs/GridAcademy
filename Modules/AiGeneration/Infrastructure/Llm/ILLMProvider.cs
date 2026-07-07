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

    /// <summary>
    /// Send a prompt and get back a plain-text completion + token counts.
    /// When <paramref name="responseSchemaJson"/> is supplied (a JSON schema string),
    /// providers that support structured output constrain the response to it — so the
    /// model must return that exact shape (e.g. an array of question objects) rather
    /// than free-form JSON. Providers without schema support ignore it.
    /// </summary>
    Task<LlmCompletion> CompleteAsync(string prompt, string? responseSchemaJson = null, CancellationToken ct = default);
}
