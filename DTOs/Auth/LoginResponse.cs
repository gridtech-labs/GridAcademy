namespace GridAcademy.DTOs.Auth;

public class LoginResponse
{
    public Guid   UserId      { get; set; }
    public string Email       { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType   { get; set; } = "Bearer";
    public DateTime ExpiresAt { get; set; }
    public string FullName    { get; set; } = string.Empty;
    public string Role        { get; set; } = string.Empty;
}
