namespace GridAcademy.Services.Marketplace;

public interface IOtpService
{
    /// <summary>Generates a 6-digit OTP and stores it in DB. Returns the OTP (for logging / SMS dispatch).</summary>
    Task<string> GenerateAsync(string contact, CancellationToken ct = default);

    /// <summary>Validates the supplied OTP. Returns true if correct and not expired.</summary>
    Task<bool> ValidateAsync(string contact, string code, CancellationToken ct = default);

    /// <summary>Deletes all expired OTP sessions (called by Hangfire cleanup job).</summary>
    Task CleanupExpiredAsync(CancellationToken ct = default);
}
