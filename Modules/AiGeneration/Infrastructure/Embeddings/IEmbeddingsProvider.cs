namespace GridAcademy.Modules.AiGeneration.Infrastructure.Embeddings;

/// <summary>
/// Abstraction over any embeddings API.
/// Returns a float[] (768-dim for Gemini text-embedding-004).
/// </summary>
public interface IEmbeddingsProvider
{
    /// <summary>Name used in llm_usage rows, e.g. "gemini".</summary>
    string ProviderName { get; }

    string ModelName { get; }

    /// <summary>
    /// Embed a single text. Returns null if the provider is unconfigured.
    /// Never throws on key-not-configured — callers degrade gracefully.
    /// </summary>
    Task<float[]?> EmbedAsync(string text, CancellationToken ct = default);
}
