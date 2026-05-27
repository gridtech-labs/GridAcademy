using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GridAcademy.Modules.AiGeneration.Infrastructure.Llm;

/// <summary>
/// Calls Google Gemini generateContent REST API.
/// Default model: gemini-2.5-flash (stable, June 2025 — 65 536 output tokens).
///
/// IMPORTANT — API key source:
///   Use a key from https://aistudio.google.com/apikey (AI Studio), NOT from
///   Google Cloud Console. AI Studio keys include the free tier.
///   Cloud Console keys default to limit=0 free-tier quota and return 429.
///
/// To list models available for your key:
///   GET https://generativelanguage.googleapis.com/v1beta/models?key=YOUR_KEY
/// </summary>
public sealed class GeminiLlmProvider : ILLMProvider
{
    private readonly HttpClient   _http;
    private readonly string       _apiKey;
    private readonly string       _baseUrl;
    private readonly ILogger<GeminiLlmProvider> _log;

    public string ProviderName => "gemini";
    public string ModelName    { get; }

    // Max number of 429 retries before giving up.
    private const int MaxRetries = 3;

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

        // Default: gemini-2.5-flash — current stable flagship (June 2025).
        // 65 536 output token limit handles large question batches without truncation.
        // Override via Railway Variable: Ai__Gemini__GenerationModel
        ModelName = cfg["Ai:Gemini:GenerationModel"] ?? "gemini-2.5-flash";

        // NOTE: do NOT throw here — constructor exceptions prevent Hangfire from
        // resolving the job, so generation_jobs.status stays "Queued" forever.
        // Validation is in CompleteAsync so RunJobAsync can catch and persist status=Failed.
        if (string.IsNullOrWhiteSpace(_apiKey))
            _log.LogWarning(
                "Ai:Gemini:ApiKey is empty — LLM calls will fail. " +
                "Set env var  Ai__Gemini__ApiKey  in Railway → Variables. " +
                "Get a free key at https://aistudio.google.com/apikey");
    }

    public async Task<LlmCompletion> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        // Validate here (not constructor) so Hangfire can resolve the service and
        // RunJobAsync can catch this and write status = Failed to the DB.
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException(
                "Ai:Gemini:ApiKey is not configured. " +
                "Get a FREE key at https://aistudio.google.com/apikey and set env var " +
                "Ai__Gemini__ApiKey in Railway → Variables.");

        var url = $"{_baseUrl}/models/{ModelName}:generateContent?key={_apiKey}";

        var bodyObj = new
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
                maxOutputTokens  = 16384,  // gemini-2.5-flash supports up to 65536
                responseMimeType = "application/json"
            }
        };

        var bodyJson = JsonSerializer.Serialize(bodyObj, _json);

        // ── Retry loop for 429 rate-limit responses ───────────────────────────
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

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

            // ── 429 Rate Limited ──────────────────────────────────────────────
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                var errBody = await response.Content.ReadAsStringAsync(ct);

                if (attempt == MaxRetries)
                {
                    // Surface a clear, actionable error in the Jobs page error row.
                    var hint = errBody.Contains("limit: 0")
                        ? "Your API key has 0 free-tier quota. " +
                          "Get a key from https://aistudio.google.com/apikey (NOT Cloud Console) " +
                          "or enable billing on your Google Cloud project."
                        : "Gemini rate limit hit. Reduce Count or wait before re-queuing.";

                    _log.LogError(
                        "Gemini 429 after {Max} attempts for model {Model}. Hint: {Hint}",
                        MaxRetries, ModelName, hint);
                    throw new HttpRequestException($"Gemini 429 rate limit — {hint}");
                }

                var delaySec = ParseRetryDelaySecs(errBody);
                _log.LogWarning(
                    "Gemini 429 (attempt {Attempt}/{Max}, model {Model}) — waiting {Delay}s…",
                    attempt, MaxRetries, ModelName, delaySec);

                await Task.Delay(TimeSpan.FromSeconds(delaySec), ct);
                continue; // retry
            }

            // ── 404 Model Not Found ────────────────────────────────────────────
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var errBody = await response.Content.ReadAsStringAsync(ct);
                _log.LogError("Gemini 404 for model {Model}: {Body}", ModelName, errBody);
                throw new HttpRequestException(
                    $"Gemini model '{ModelName}' not found (404). " +
                    $"To see available models for your key, open: " +
                    $"{_baseUrl}/models?key=<your-key> — then set " +
                    $"Ai__Gemini__GenerationModel in Railway Variables to a working model name.");
            }

            // ── Other HTTP error ───────────────────────────────────────────────
            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync(ct);
                _log.LogError("Gemini API error {Status}: {Body}", response.StatusCode, errBody);
                throw new HttpRequestException(
                    $"Gemini API returned {(int)response.StatusCode}: {errBody}");
            }

            // ── Success ────────────────────────────────────────────────────────
            var json = await response.Content.ReadAsStringAsync(ct);
            var root = JsonNode.Parse(json)!;

            var text = root["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>()
                       ?? throw new InvalidOperationException($"Unexpected Gemini response: {json}");

            var usage        = root["usageMetadata"];
            var promptTokens = usage?["promptTokenCount"]?.GetValue<int>()     ?? 0;
            var outputTokens = usage?["candidatesTokenCount"]?.GetValue<int>() ?? 0;

            return new LlmCompletion(text, promptTokens, outputTokens, ModelName);
        }

        // Should never be reached (loop always returns or throws).
        throw new InvalidOperationException("Gemini request failed after all retries.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses "Please retry in 58.59s." or "retry in 58s" from the error body.
    /// Falls back to 65 s if the pattern is not found.
    /// </summary>
    private static double ParseRetryDelaySecs(string errBody)
    {
        var m = Regex.Match(errBody, @"retry in (\d+\.?\d*)s",
                            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (m.Success &&
            double.TryParse(m.Groups[1].Value,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var secs))
        {
            return Math.Min(secs + 2, 120); // +2 s buffer, cap at 2 min
        }
        return 65;
    }
}
