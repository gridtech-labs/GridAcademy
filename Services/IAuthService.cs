using GridAcademy.DTOs.Auth;

namespace GridAcademy.Services;

public interface IAuthService
{
    /// <summary>Validates credentials and returns a JWT on success.</summary>
    Task<LoginResponse> LoginAsync(LoginRequest request);
}
