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

    /// <summary>
    /// Send a prompt together with a file (e.g. a question-bank PDF) and get back a
    /// completion. The model reads the document itself, so maths and figures that plain
    /// text extraction mangles are understood. <paramref name="responseSchemaJson"/>
    /// works as in <see cref="CompleteAsync"/>. Providers without file support throw
    /// <see cref="NotSupportedException"/>.
    /// </summary>
    Task<LlmCompletion> CompleteWithFileAsync(
        string prompt, byte[] fileBytes, string mimeType,
        string? responseSchemaJson = null, CancellationToken ct = default);
}
