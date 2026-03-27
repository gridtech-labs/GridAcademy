namespace GridAcademy.Services.Marketplace;

public interface IRazorpayService
{
    /// <summary>Creates a Razorpay order and returns the Razorpay order_id.</summary>
    Task<string> CreateOrderAsync(decimal amountInr, string receipt, CancellationToken ct = default);

    /// <summary>Verifies the HMAC-SHA256 payment signature from Razorpay callback.</summary>
    bool VerifySignature(string razorpayOrderId, string razorpayPaymentId, string razorpaySignature);
}
