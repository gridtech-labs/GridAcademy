using System.Net.Http.Headers;
using System.Text.Json;

namespace GridAcademy.Services;

/// <summary>
/// Mathpix PDF OCR integration.
///
/// Workflow:
///   1. POST /v3/pdf          — upload the PDF, receive a pdf_id
///   2. GET  /v3/pdf/{id}     — poll status every 3 s (max 3 min)
///   3. GET  /v3/pdf/{id}.mmd — download Mathpix Markdown when status = "completed"
///
/// Configure in appsettings.json:
///   "Mathpix": { "AppId": "your_app_id", "AppKey": "your_app_key" }
/// </summary>
public class MathpixService : IMathpixService
{
    private const string BaseUrl      = "https://api.mathpix.com/v3";
    private const int    PollInterval = 3_000;   // ms between status checks
    private const int    MaxAttempts  = 60;       // 60 × 3 s = 3 minutes max

    private readonly IHttpClientFactory       _httpFactory;
    private readonly ILogger<MathpixService>  _logger;
    private readonly string?                  _appId;
    private readonly string?                  _appKey;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_appId) && !string.IsNullOrWhiteSpace(_appKey);

    public MathpixService(
        IHttpClientFactory      httpFactory,
        IConfiguration          config,
        ILogger<MathpixService> logger)
    {
        _httpFactory = httpFactory;
        _logger      = logger;
        _appId       = config["Mathpix:AppId"];
        _appKey      = config["Mathpix:AppKey"];
    }

    public async Task<string> OcrPdfAsync(byte[] pdfBytes, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException(
                "Mathpix API is not configured. " +
                "Add Mathpix:AppId and Mathpix:AppKey to appsettings.json, " +
                "then restart the application.");

        var http = _httpFactory.CreateClient("mathpix");

        // ── Step 1: Submit PDF ────────────────────────────────────────────────
        var pdfId = await SubmitPdfAsync(http, pdfBytes, ct);
        _logger.LogInformation("Mathpix: submitted PDF, pdf_id = {PdfId}", pdfId);

        // ── Step 2: Poll until completed ──────────────────────────────────────
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            await Task.Delay(PollInterval, ct);

            var (status, error) = await GetStatusAsync(http, pdfId, ct);
            _logger.LogDebug("Mathpix: {PdfId} attempt {Attempt} → {Status}", pdfId, attempt, status);

            if (status == "completed")
            {
                // ── Step 3: Download MMD text ─────────────────────────────────
                _logger.LogInformation("Mathpix: {PdfId} completed, downloading MMD", pdfId);
                return await DownloadMmdAsync(http, pdfId, ct);
            }

            if (status == "error")
                throw new InvalidOperationException($"Mathpix processing failed: {error ?? "unknown error"}");
        }

        throw new TimeoutException(
            $"Mathpix did not finish processing within {MaxAttempts * PollInterval / 1000} seconds. " +
            "Try re-importing or check the Mathpix dashboard.");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<string> SubmitPdfAsync(HttpClient http, byte[] pdfBytes, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", "paper.pdf");

        // Request MMD output with LaTeX delimiters $…$ and $$…$$
        form.Add(new StringContent(
            """{"conversion_formats":{"mmd":true},"math_inline_delimiters":["$","$"],"math_display_delimiters":["$$","$$"]}"""),
            "options_json");

        using var req = BuildRequest(HttpMethod.Post, $"{BaseUrl}/pdf", form);
        var res  = await http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Mathpix submission failed (HTTP {(int)res.StatusCode}): {body}");

        return JsonDocument.Parse(body).RootElement
                   .GetProperty("pdf_id").GetString()
               ?? throw new InvalidOperationException("Mathpix response missing pdf_id.");
    }

    private async Task<(string Status, string? Error)> GetStatusAsync(
        HttpClient http, string pdfId, CancellationToken ct)
    {
        using var req = BuildRequest(HttpMethod.Get, $"{BaseUrl}/pdf/{pdfId}");
        var res  = await http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        var root   = JsonDocument.Parse(body).RootElement;
        var status = root.GetProperty("status").GetString() ?? "unknown";
        var error  = root.TryGetProperty("error", out var e) ? e.GetString() : null;
        return (status, error);
    }

    private async Task<string> DownloadMmdAsync(HttpClient http, string pdfId, CancellationToken ct)
    {
        using var req = BuildRequest(HttpMethod.Get, $"{BaseUrl}/pdf/{pdfId}.mmd");
        var res  = await http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStringAsync(ct);
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string url, HttpContent? content = null)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add("app_id",  _appId);
        req.Headers.Add("app_key", _appKey);
        if (content != null) req.Content = content;
        return req;
    }
}
