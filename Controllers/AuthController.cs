using GridAcademy.Common;
using GridAcademy.Data;
using GridAcademy.Data.Entities;
using GridAcademy.DTOs.Auth;
using GridAcademy.DTOs.Marketplace;
using GridAcademy.Helpers;
using GridAcademy.Services;
using GridAcademy.Services.Marketplace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService   _auth;
    private readonly IOtpService    _otp;
    private readonly AppDbContext   _db;
    private readonly JwtHelper      _jwt;

    public AuthController(IAuthService auth, IOtpService otp, AppDbContext db, JwtHelper jwt)
    {
        _auth = auth;
        _otp  = otp;
        _db   = db;
        _jwt  = jwt;
    }

    /// <summary>
    /// Roles that may never sign in through passwordless quick-access — they hold
    /// privileged access and always have a real password.
    /// </summary>
    private static readonly string[] StaffRoles =
        ["Admin", "SuperAdmin", "Instructor", "Provider"];

    /// <summary>
    /// Digits only, last 10 — so "+91 98765 43210", "09876543210" and "9876543210"
    /// all compare equal.
    /// </summary>
    private static string NormalizeMobile(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length > 10 ? digits[^10..] : digits;
    }

    // ── Email + Password Login ────────────────────────────────────────────────

    /// <summary>
    /// Authenticate with email and password. Returns a JWT access token.
    /// </summary>
    /// <remarks>Default seeded admin: admin@gridacademy.com / Admin@123!</remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _auth.LoginAsync(request);
        return Ok(ApiResponse<LoginResponse>.Ok(response, "Login successful."));
    }

    // ── Student Registration ──────────────────────────────────────────────────

    /// <summary>Register a new student account (marketplace).</summary>
    [HttpPost("register/student")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterStudent([FromBody] StudentRegisterRequest req)
    {
        var emailTaken = await _db.Users.AnyAsync(u => u.Email == req.Email.Trim().ToLower());
        if (emailTaken)
            return BadRequest(ApiResponse.Fail("An account with this email already exists."));

        var pwError = PasswordHelper.Validate(req.Password);
        if (pwError is not null)
            return BadRequest(ApiResponse.Fail(pwError));

        var user = new User
        {
            FirstName    = req.FirstName.Trim(),
            LastName     = req.LastName.Trim(),
            Email        = req.Email.Trim().ToLower(),
            Phone        = req.Phone?.Trim(),
            PasswordHash = PasswordHelper.Hash(req.Password),
            Role         = "Student",
            IsActive     = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var (token, expiresAt) = _jwt.GenerateToken(user);
        var loginResp = new LoginResponse { UserId = user.Id, Email = user.Email, AccessToken = token, ExpiresAt = expiresAt, FullName = user.FullName, Role = user.Role };
        return StatusCode(201, ApiResponse<LoginResponse>.Ok(loginResp, "Account created successfully."));
    }

    // ── Provider Registration ─────────────────────────────────────────────────

    /// <summary>Register a new provider account (marketplace).</summary>
    [HttpPost("register/provider")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterProvider([FromBody] ProviderRegisterRequest req)
    {
        if (!req.AgreedToTerms)
            return BadRequest(ApiResponse.Fail("You must agree to the terms and conditions."));

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == req.Email.Trim().ToLower());
        if (emailTaken)
            return BadRequest(ApiResponse.Fail("An account with this email already exists."));

        var pwError = PasswordHelper.Validate(req.Password);
        if (pwError is not null)
            return BadRequest(ApiResponse.Fail(pwError));

        var user = new User
        {
            FirstName    = req.FirstName.Trim(),
            LastName     = req.LastName.Trim(),
            Email        = req.Email.Trim().ToLower(),
            Phone        = req.Phone?.Trim(),
            PasswordHash = PasswordHelper.Hash(req.Password),
            Role         = "Provider",
            IsActive     = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Create the provider profile
        _db.MpProviders.Add(new Data.Entities.Marketplace.MpProvider
        {
            UserId        = user.Id,
            InstituteName = req.InstituteName.Trim(),
            City          = req.City,
            State         = req.State,
            Bio           = req.Bio,
            AgreedToTerms = true,
            AgreedAt      = DateTime.UtcNow,
            Status        = Data.Entities.Marketplace.ProviderStatus.Pending,
            CreatedAt     = DateTime.UtcNow,
            UpdatedAt     = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var (token, expiresAt) = _jwt.GenerateToken(user);
        var loginResp = new LoginResponse { UserId = user.Id, Email = user.Email, AccessToken = token, ExpiresAt = expiresAt, FullName = user.FullName, Role = user.Role };
        return StatusCode(201, ApiResponse<LoginResponse>.Ok(loginResp, "Provider account created. Pending admin verification."));
    }

    // ── Quick Access (frictionless student entry) ─────────────────────────────

    /// <summary>
    /// Frictionless student entry — no password needed.
    /// If the email already exists: silently login and return a JWT.
    /// If the email is new: auto-register as Student with a random password, then return a JWT.
    /// Mobile number is stored for future OTP login.
    /// </summary>
    [HttpPost("quick-access")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> QuickAccess([FromBody] QuickAccessRequest req, CancellationToken ct)
    {
        var email  = req.Email.Trim().ToLower();
        var mobile = req.Mobile.Trim();

        // Find existing user by email
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
        {
            // Auto-register — derive a friendly name from the email prefix
            var namePart  = email.Split('@')[0];
            var firstName = char.ToUpper(namePart[0]) + (namePart.Length > 1 ? namePart[1..].Split('.', '_', '-')[0] : "");

            user = new Data.Entities.User
            {
                FirstName    = firstName,
                LastName     = "Student",
                Email        = email,
                Phone        = mobile,
                PasswordHash = PasswordHelper.Hash(Guid.NewGuid().ToString()), // random — not used
                Role         = "Student",
                IsActive     = true
            };
            _db.Users.Add(user);
        }
        else
        {
            // SECURITY: this endpoint is passwordless, so knowing an email address must
            // never be enough to sign in to somebody else's account.

            // 1. Staff accounts are off-limits here — they sign in with their password.
            //    Without this, anyone could enter admin@… and receive an admin token.
            if (StaffRoles.Contains(user.Role, StringComparer.OrdinalIgnoreCase))
                return Unauthorized(ApiResponse.Fail(
                    "This email is registered as a staff account. Please sign in with your password."));

            if (!user.IsActive)
                return BadRequest(ApiResponse.Fail("Your account has been deactivated. Please contact support."));

            // 2. The mobile number must match the one held on the account. Accounts
            //    created before a number was captured are bound to the one given here,
            //    so existing students are not locked out.
            var storedMobile = NormalizeMobile(user.Phone);
            if (storedMobile.Length == 0)
                user.Phone = mobile;
            else if (storedMobile != NormalizeMobile(mobile))
                return Unauthorized(ApiResponse.Fail(
                    "An account already exists for this email. Enter the mobile number registered " +
                    "with it, or sign in with your password."));
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var (token, expiresAt) = _jwt.GenerateToken(user);
        var resp = new LoginResponse
        {
            UserId      = user.Id,
            Email       = user.Email,
            AccessToken = token,
            ExpiresAt   = expiresAt,
            FullName    = user.FullName,
            Role        = user.Role
        };

        return Ok(ApiResponse<LoginResponse>.Ok(resp, "Welcome! You're all set."));
    }

    // ── OTP ───────────────────────────────────────────────────────────────────

    /// <summary>Send a 6-digit OTP to a mobile number or email for passwordless login.</summary>
    [HttpPost("otp/send")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest req, CancellationToken ct)
    {
        await _otp.GenerateAsync(req.Contact, ct);
        return Ok(ApiResponse.Ok("OTP sent successfully."));
    }

    /// <summary>Verify the OTP and return a JWT if the contact matches an active user.</summary>
    [HttpPost("otp/verify")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest req, CancellationToken ct)
    {
        var valid = await _otp.ValidateAsync(req.Contact, req.OtpCode, ct);
        if (!valid)
            return Unauthorized(ApiResponse.Fail("Invalid or expired OTP."));

        // Match user by phone or email
        var contact = req.Contact.Trim().ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.IsActive && (u.Email == contact || u.Phone == contact), ct);

        if (user is null)
            return Unauthorized(ApiResponse.Fail("No active account found for this contact."));

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var (token, expiresAt) = _jwt.GenerateToken(user);
        var resp = new LoginResponse { UserId = user.Id, Email = user.Email, AccessToken = token, ExpiresAt = expiresAt, FullName = user.FullName, Role = user.Role };
        return Ok(ApiResponse<LoginResponse>.Ok(resp, "OTP verified. Login successful."));
    }
}
