using GridAcademy.Data;
using GridAcademy.Data.Entities.Marketplace;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services.Marketplace;

/// <summary>
/// OTP management backed by PostgreSQL (MpOtpSession table).
/// TTL = 10 minutes; max 5 attempts per session before it is locked.
/// In development, OTP is written to the console log.
/// Wire up an SMS provider (MSG91 / Twilio) by replacing the TODO stub.
/// </summary>
public class OtpService : IOtpService
{
    private const int OtpTtlMinutes  = 10;
    private const int MaxAttempts    = 5;

    private readonly AppDbContext _db;
    private readonly ILogger<OtpService> _logger;

    public OtpService(AppDbContext db, ILogger<OtpService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ── Generate ─────────────────────────────────────────────────────────────
    public async Task<string> GenerateAsync(string contact, CancellationToken ct = default)
    {
        contact = contact.Trim().ToLower();

        // Invalidate any existing unused OTP for this contact
        var existing = await _db.MpOtpSessions
            .Where(o => o.Contact == contact && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);
        _db.MpOtpSessions.RemoveRange(existing);

        // Generate a 6-digit code
        var code = Random.Shared.Next(100_000, 999_999).ToString();

        _db.MpOtpSessions.Add(new MpOtpSession
        {
            Contact      = contact,
            OtpCode      = code,
            AttemptCount = 0,
            IsUsed       = false,
            ExpiresAt    = DateTime.UtcNow.AddMinutes(OtpTtlMinutes),
            CreatedAt    = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        // ── SMS stub — replace with MSG91 / Twilio in production ────────────
        _logger.LogWarning("OTP for {Contact}: {Code}  [DEV — send via SMS in production]", contact, code);
        // TODO: await _smsClient.SendAsync(contact, $"Your GridAcademy OTP is {code}. Valid for {OtpTtlMinutes} minutes.");

        return code;
    }

    // ── Validate ─────────────────────────────────────────────────────────────
    public async Task<bool> ValidateAsync(string contact, string code, CancellationToken ct = default)
    {
        contact = contact.Trim().ToLower();
        code    = code.Trim();

        var session = await _db.MpOtpSessions
            .Where(o => o.Contact == contact && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (session is null) return false;

        // Increment attempt count regardless of correctness (brute-force guard)
        session.AttemptCount++;

        if (session.AttemptCount > MaxAttempts)
        {
            // Lock: mark as used so it can't be retried
            session.IsUsed = true;
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning("OTP max attempts exceeded for {Contact}", contact);
            return false;
        }

        if (session.OtpCode != code)
        {
            await _db.SaveChangesAsync(ct);
            return false;
        }

        // Mark as used — prevents replay
        session.IsUsed = true;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("OTP validated successfully for {Contact}", contact);
        return true;
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────
    public async Task CleanupExpiredAsync(CancellationToken ct = default)
    {
        var cutoff  = DateTime.UtcNow;
        var expired = await _db.MpOtpSessions
            .Where(o => o.ExpiresAt < cutoff || o.IsUsed)
            .ToListAsync(ct);

        if (expired.Count > 0)
        {
            _db.MpOtpSessions.RemoveRange(expired);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Cleaned up {Count} expired OTP sessions.", expired.Count);
        }
    }
}
