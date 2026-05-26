using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GridAcademy.Modules.AiGeneration.Infrastructure.Embeddings;

/// <summary>
/// Generates text embeddings using Google Gemini text-embedding-004.
/// Vector dimension: 768. Used for duplicate detection via pgvector cosine similarity.
/// </summary>
public sealed class GeminiEmbeddingsProvider : IEmbeddingsProvider
{
    private readonly HttpClient   _http;
    private readonly string?      _apiKey;
    private readonly string       _baseUrl;
    private readonly ILogger<GeminiEmbeddingsProvider> _log;

    public string ProviderName => "gemini";
    public string ModelName    { get; }

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GeminiEmbeddingsProvider(HttpClient http, IConfiguration cfg, ILogger<GeminiEmbeddingsProvider> log)
    {
        _http     = http;
        _log      = log;
        _apiKey   = cfg["Ai:Gemini:ApiKey"];
        _baseUrl  = cfg["Ai:Gemini:BaseUrl"]         ?? "https://generativelanguage.googleapis.com/v1beta";
        ModelName = cfg["Ai:Gemini:EmbeddingsModel"] ?? "text-embedding-004";
    }

    public async Task<float[]?> EmbedAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _log.LogDebug("Gemini API key not configured — embeddings skipped.");
            return null;
        }

        var url = $"{_baseUrl}/models/{ModelName}:embedContent?key={_apiKey}";

        var body = new
        {
            model   = $"models/{ModelName}",
            content = new
            {
                parts = new[] { new { text } }
            },
            taskType = "RETRIEVAL_DOCUMENT"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(body, _json),
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsync(url, content, ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Gemini embeddings HTTP call failed — duplicate check will be skipped.");
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            _log.LogWarning("Gemini embeddings API {Status}: {Err} — duplicate check skipped.", response.StatusCode, err);
            return null;
        }

        var json   = await response.Content.ReadAsStringAsync(ct);
        var root   = JsonNode.Parse(json);
        var values = root?["embedding"]?["values"]?.AsArray();

        if (values is null)
        {
            _log.LogWarning("Unexpected Gemini embeddings response shape — duplicate check skipped.");
            return null;
        }

        return values.Select(v => v!.GetValue<float>()).ToArray();
    }
}
