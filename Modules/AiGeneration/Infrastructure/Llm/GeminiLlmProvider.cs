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

    // Max attempts for retryable errors (429 rate-limit, 503 overload).
    private const int MaxRetries = 5;

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

        // Default: gemini-2.5-flash — verified to work with AI Studio keys
        // (gemini-2.0-flash 404s for many keys). Thinking is disabled per-request
        // (thinkingBudget=0) so it stays fast. Override via Railway Variable:
        // Ai__Gemini__GenerationModel
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

        // Build generationConfig as a dictionary so thinkingConfig can be added
        // conditionally (only thinking-capable models accept it).
        var generationConfig = new Dictionary<string, object?>
        {
            ["temperature"]      = 0.7,
            ["maxOutputTokens"]  = 16384,   // headroom; gemini-2.5-flash allows up to 65536
            ["responseMimeType"] = "application/json"
        };

        // gemini-2.5+/3.x "flash" models have thinking ON by default, which adds
        // 5–15 min of latency per call. Disable it. Older/lite models that don't
        // support thinking would reject this field, so only send it when relevant.
        if (SupportsThinking(ModelName))
            generationConfig["thinkingConfig"] = new { thinkingBudget = 0 };

        var bodyObj = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig
        };

        var bodyJson = JsonSerializer.Serialize(bodyObj, _json);

        // ── Retry loop: handles 429 (rate-limit) and 503 (overload) ─────────
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

            // ── 503 Service Unavailable (transient overload) ──────────────────
            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                if (attempt == MaxRetries)
                {
                    _log.LogError(
                        "Gemini 503 after {Max} attempts for model {Model}. Giving up.",
                        MaxRetries, ModelName);
                    throw new HttpRequestException(
                        $"Gemini 503 — service temporarily unavailable after {MaxRetries} retries. " +
                        "Re-queue the job in a few minutes.");
                }

                // Exponential backoff: 10s, 20s, 40s, 80s
                var delaySec = (int)Math.Pow(2, attempt) * 5;
                _log.LogWarning(
                    "Gemini 503 overload (attempt {Attempt}/{Max}, model {Model}) — waiting {Delay}s…",
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

            var candidate    = root["candidates"]?[0];
            var finishReason = candidate?["finishReason"]?.GetValue<string>();

            // Gemini 2.5+ thinking models return multiple parts:
            //   parts[0] = thinking trace  (has "thought": true)  ← skip this
            //   parts[1] = actual response (no "thought" field)   ← use this
            // With thinkingBudget=0 there is only one part, but we search defensively.
            var parts = candidate?["content"]?["parts"]?.AsArray();
            string? text = null;
            if (parts != null)
            {
                foreach (var part in parts)
                {
                    if (part?["thought"]?.GetValue<bool>() == true) continue; // skip thinking trace
                    text = part?["text"]?.GetValue<string>();
                    if (text != null) break;
                }
            }

            if (text == null)
            {
                // Give an actionable message based on why generation stopped.
                var reasonHint = finishReason switch
                {
                    "MAX_TOKENS" => "the model hit the output-token limit before returning any text. " +
                                    "Reduce the batch size (Ai:Generation:MaxQuestionsPerBatch).",
                    "SAFETY"     => "the response was blocked by Gemini safety filters. Try a different topic/prompt.",
                    "RECITATION" => "the response was blocked for recitation (copyright). Rephrase the prompt.",
                    "PROHIBITED_CONTENT" => "the response was blocked as prohibited content.",
                    null         => "no candidates were returned.",
                    _            => $"finishReason = {finishReason}."
                };
                _log.LogError("Gemini returned no usable text ({Reason}). Body: {Body}",
                    reasonHint, json[..Math.Min(600, json.Length)]);
                throw new InvalidOperationException($"Gemini returned no question text — {reasonHint}");
            }

            // Truncated mid-JSON — the array will not parse. Fail with a clear cause.
            if (finishReason == "MAX_TOKENS")
                _log.LogWarning(
                    "Gemini response was truncated (MAX_TOKENS) for model {Model}. " +
                    "Consider lowering Ai:Generation:MaxQuestionsPerBatch.", ModelName);

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
    /// True for Gemini models that have "thinking" enabled by default (2.5+, 3.x).
    /// These need thinkingBudget=0 to respond quickly; older models reject the field.
    /// </summary>
    private static bool SupportsThinking(string model)
    {
        var m = model.ToLowerInvariant();
        if (m.Contains("flash-latest") || m.Contains("pro-latest")) return true;
        // gemini-2.5-*, gemini-3-*, gemini-3.1-*, gemini-3.5-* …
        return m.Contains("gemini-2.5") || m.Contains("gemini-3");
    }

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
