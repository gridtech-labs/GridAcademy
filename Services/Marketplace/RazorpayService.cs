using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GridAcademy.Services.Marketplace;

/// <summary>
/// Thin wrapper around the Razorpay REST API v1.
/// Uses HttpClient + Basic auth (key_id:key_secret) — no extra NuGet required.
/// </summary>
public class RazorpayService : IRazorpayService
{
    private const string BaseUrl = "https://api.razorpay.com/v1/";

    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<RazorpayService> _logger;

    public RazorpayService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<RazorpayService> logger)
    {
        _http   = httpFactory.CreateClient("Razorpay");
        _config = config;
        _logger = logger;
    }

    // ── Create Order ─────────────────────────────────────────────────────────
    public async Task<string> CreateOrderAsync(decimal amountInr, string receipt, CancellationToken ct = default)
    {
        var keyId     = _config["Razorpay:KeyId"]     ?? throw new InvalidOperationException("Razorpay:KeyId not configured.");
        var keySecret = _config["Razorpay:KeySecret"] ?? throw new InvalidOperationException("Razorpay:KeySecret not configured.");

        // Razorpay amounts are in paise (1 INR = 100 paise)
        var amountPaise = (int)(amountInr * 100);

        var payload = new
        {
            amount   = amountPaise,
            currency = "INR",
            receipt  = receipt
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}orders")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        // Basic auth: base64(key_id:key_secret)
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{keySecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var response = await _http.SendAsync(request, ct);
        var body     = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Razorpay order creation failed: {Status} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Razorpay error: {response.StatusCode}");
        }

        using var doc     = JsonDocument.Parse(body);
        var razorpayOrderId = doc.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Razorpay response missing 'id' field.");

        _logger.LogInformation("Razorpay order created: {OrderId}", razorpayOrderId);
        return razorpayOrderId;
    }

    // ── Verify Signature ─────────────────────────────────────────────────────
    public bool VerifySignature(string razorpayOrderId, string razorpayPaymentId, string razorpaySignature)
    {
        var keySecret = _config["Razorpay:KeySecret"]
            ?? throw new InvalidOperationException("Razorpay:KeySecret not configured.");

        // HMAC-SHA256 of "order_id|payment_id" using key_secret
        var message = $"{razorpayOrderId}|{razorpayPaymentId}";
        var keyBytes = Encoding.UTF8.GetBytes(keySecret);
        var msgBytes = Encoding.UTF8.GetBytes(message);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(msgBytes);
        var computed = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        var valid = computed == razorpaySignature.ToLowerInvariant();
        if (!valid) _logger.LogWarning("Razorpay signature mismatch for order {OrderId}", razorpayOrderId);
        return valid;
    }
}
