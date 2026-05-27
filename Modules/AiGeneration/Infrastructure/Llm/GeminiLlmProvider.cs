using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GridAcademy.Modules.AiGeneration.Infrastructure.Llm;

/// <summary>
/// Calls Google Gemini generateContent REST API.
/// Model: gemini-2.0-flash (configurable via Ai:Gemini:GenerationModel).
/// </summary>
public sealed class GeminiLlmProvider : ILLMProvider
{
    private readonly HttpClient   _http;
    private readonly string       _apiKey;
    private readonly string       _baseUrl;
    private readonly ILogger<GeminiLlmProvider> _log;

    public string ProviderName => "gemini";
    public string ModelName    { get; }

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GeminiLlmProvider(HttpClient http, IConfiguration cfg, ILogger<GeminiLlmProvider> log)
    {
        _http     = http;
        _log      = log;
        _apiKey   = cfg["Ai:Gemini:ApiKey"] ?? "";
        _baseUrl  = cfg["Ai:Gemini:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta";
        ModelName = cfg["Ai:Gemini:GenerationModel"] ?? "gemini-2.0-flash";

        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException(
                "Ai:Gemini:ApiKey is not configured. " +
                "Set the environment variable  Ai__Gemini__ApiKey  in Railway → Variables.");
    }

    public async Task<LlmCompletion> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/models/{ModelName}:generateContent?key={_apiKey}";

        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature      = 0.7,
                maxOutputTokens  = 4096,
                responseMimeType = "application/json"
            }
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
            _log.LogError(ex, "Gemini HTTP request failed for model {Model}", ModelName);
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync(ct);
            _log.LogError("Gemini API error {Status}: {Body}", response.StatusCode, errBody);
            throw new HttpRequestException(
                $"Gemini API returned {(int)response.StatusCode}: {errBody}");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var root = JsonNode.Parse(json)!;

        // Extract generated text
        var text = root["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>()
                   ?? throw new InvalidOperationException($"Unexpected Gemini response: {json}");

        // Extract token counts (may be absent for some model variants)
        var usage         = root["usageMetadata"];
        var promptTokens  = usage?["promptTokenCount"]?.GetValue<int>()    ?? 0;
        var outputTokens  = usage?["candidatesTokenCount"]?.GetValue<int>() ?? 0;

        return new LlmCompletion(text, promptTokens, outputTokens, ModelName);
    }
}
