using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GridAcademy.Services.ExamContent.Options;
using Microsoft.Extensions.Options;

namespace GridAcademy.Services.ExamContent.AI;

public class OpenAiApiClient(
    IHttpClientFactory httpClientFactory,
    IOptions<AiRewriteOptions> options,
    ILogger<OpenAiApiClient> logger) : IAiApiClient
{
    private readonly AiRewriteOptions _options = options.Value;

    public async Task<(string Html, AiTokenUsage? Usage)> GenerateHtmlAsync(string prompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("AIRewrite:ApiKey is not configured.");

        var http = httpClientFactory.CreateClient("OpenAI");
        http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        var payload = new
        {
            model = _options.Model,
            temperature = _options.Temperature,
            messages = new[]
            {
                new { role = "system", content = "You only return valid, clean HTML and never markdown." },
                new { role = "user", content = prompt }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("OpenAI call failed: {StatusCode} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"AI provider call failed with HTTP {(int)response.StatusCode}.");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var html = root
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(html))
            throw new InvalidOperationException("AI provider returned empty content.");

        AiTokenUsage? usage = null;
        if (root.TryGetProperty("usage", out var usageElement))
        {
            usage = new AiTokenUsage(
                usageElement.TryGetProperty("prompt_tokens", out var p) ? p.GetInt32() : null,
                usageElement.TryGetProperty("completion_tokens", out var c) ? c.GetInt32() : null,
                usageElement.TryGetProperty("total_tokens", out var t) ? t.GetInt32() : null);
        }

        return (html.Trim(), usage);
    }
}
