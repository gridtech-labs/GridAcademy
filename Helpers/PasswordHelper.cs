namespace GridAcademy.Helpers;

/// <summary>
/// Thin wrapper around BCrypt. Work factor 12 is a good balance
/// of security and performance for most workloads.
/// </summary>
public static class PasswordHelper
{
    private const int WorkFactor = 12;

    /// <summary>Returns a BCrypt hash of the plain-text password.</summary>
    public static string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    /// <summary>Verifies a plain-text password against a stored BCrypt hash.</summary>
    public static bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);

    /// <summary>
    /// Basic password strength check.
    /// Returns an error message or null if password is acceptable.
    /// </summary>
    public static string? Validate(string password)
    {
        if (password.Length < 8)
            return "Password must be at least 8 characters.";
        if (!password.Any(char.IsUpper))
            return "Password must contain at least one uppercase letter.";
        if (!password.Any(char.IsDigit))
            return "Password must contain at least one digit.";
        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            return "Password must contain at least one special character.";
        return null;
    }
}
