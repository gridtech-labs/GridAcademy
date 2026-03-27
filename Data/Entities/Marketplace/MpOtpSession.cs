using System.ComponentModel.DataAnnotations;

namespace GridAcademy.Data.Entities.Marketplace;

/// <summary>
/// Short-lived OTP session. Stored in DB for simplicity;
/// auto-cleaned by a Hangfire job after expiry.
/// </summary>
public class MpOtpSession
{
    public int Id { get; set; }

    /// <summary>Mobile number or email address the OTP was sent to.</summary>
    [Required, MaxLength(200)]
    public string Contact { get; set; } = string.Empty;

    [Required, MaxLength(6)]
    public string OtpCode { get; set; } = string.Empty;

    public int AttemptCount { get; set; } = 0;

    public bool IsUsed { get; set; } = false;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
