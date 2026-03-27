using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GridAcademy.Data.Entities;
using Microsoft.IdentityModel.Tokens;

namespace GridAcademy.Helpers;

/// <summary>
/// Generates and validates JWT access tokens.
/// Config is read from appsettings.json → Jwt section.
/// </summary>
public class JwtHelper
{
    private readonly IConfiguration _config;

    public JwtHelper(IConfiguration config) => _config = config;

    /// <summary>Creates a signed JWT for the given user.</summary>
    public (string token, DateTime expiresAt) GenerateToken(User user)
    {
        var secret  = _config["Jwt:Secret"]   ?? throw new InvalidOperationException("Jwt:Secret is missing.");
        var issuer  = _config["Jwt:Issuer"]   ?? "GridAcademy";
        var audience = _config["Jwt:Audience"] ?? "GridAcademy";
        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");

        var key  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name,  user.FullName),
            new Claim(ClaimTypes.Role,               user.Role),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            expires:            expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
