using GridAcademy.Data;
using GridAcademy.DTOs.Auth;
using GridAcademy.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtHelper _jwt;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, JwtHelper jwt, ILogger<AuthService> logger)
    {
        _db     = db;
        _jwt    = jwt;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        // 1. Find user by email (case-insensitive)
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower().Trim());

        // 2. Validate credentials — same error message for both cases to avoid user enumeration
        if (user is null || !PasswordHelper.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        // 3. Check account is active
        if (!user.IsActive)
            throw new UnauthorizedAccessException("Your account has been deactivated. Please contact an administrator.");

        // 4. Generate JWT
        var (token, expiresAt) = _jwt.GenerateToken(user);

        // 5. Update last login timestamp
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {Email} logged in at {Time}", user.Email, DateTime.UtcNow);

        return new LoginResponse
        {
            UserId      = user.Id,
            Email       = user.Email,
            AccessToken = token,
            ExpiresAt   = expiresAt,
            FullName    = user.FullName,
            Role        = user.Role
        };
    }
}
