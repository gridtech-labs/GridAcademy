using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GridAcademy.Modules.AiGeneration.Infrastructure.Llm;

/// <summary>
/// Stub implementation for Anthropic Claude API.
/// Activate by setting "Ai:LlmProvider": "anthropic" and filling Ai:Anthropic:ApiKey.
/// </summary>
public sealed class AnthropicLlmProvider : ILLMProvider
{
    private readonly HttpClient  _http;
    private readonly string      _apiKey;
    private readonly ILogger<AnthropicLlmProvider> _log;

    public string ProviderName => "anthropic";
    public string ModelName    { get; }

    private const string BaseUrl = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public AnthropicLlmProvider(HttpClient http, IConfiguration cfg, ILogger<AnthropicLlmProvider> log)
    {
        _http     = http;
        _log      = log;
        _apiKey   = cfg["Ai:Anthropic:ApiKey"]           ?? throw new InvalidOperationException("Ai:Anthropic:ApiKey not configured");
        ModelName = cfg["Ai:Anthropic:GenerationModel"]  ?? "claude-haiku-4-5-20251001";
    }

    // responseSchemaJson is accepted for interface parity; Anthropic tool-use/JSON
    // mode is not wired here, so it is ignored (the prompt still asks for JSON).
    public async Task<LlmCompletion> CompleteAsync(string prompt, string? responseSchemaJson = null, CancellationToken ct = default)
    {
        var body = new
        {
            model      = ModelName,
            max_tokens = 4096,
            messages   = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, _json),
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Anthropic HTTP request failed for model {Model}", ModelName);
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync(ct);
            _log.LogError("Anthropic API error {Status}: {Body}", response.StatusCode, errBody);
            throw new HttpRequestException($"Anthropic API returned {(int)response.StatusCode}: {errBody}");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var root = JsonNode.Parse(json)!;

        var text = root["content"]?[0]?["text"]?.GetValue<string>()
                   ?? throw new InvalidOperationException($"Unexpected Anthropic response: {json}");

        var usage         = root["usage"];
        var promptTokens  = usage?["input_tokens"]?.GetValue<int>()  ?? 0;
        var outputTokens  = usage?["output_tokens"]?.GetValue<int>() ?? 0;

        return new LlmCompletion(text, promptTokens, outputTokens, ModelName);
    }
}
