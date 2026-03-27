namespace GridAcademy.DTOs.Users;

/// <summary>Query parameters for listing users with optional search and pagination.</summary>
public class UserListRequest
{
    public string? Search { get; set; }       // Filter by name or email
    public string? Role { get; set; }         // Filter by role
    public bool? IsActive { get; set; }       // Filter by active status
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
