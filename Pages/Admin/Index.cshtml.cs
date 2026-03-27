using GridAcademy.Data;
using GridAcademy.DTOs.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Admin;

[Authorize(Roles = "Admin,Instructor")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    // Admin stats
    public int TotalUsers       { get; set; }
    public int AdminCount       { get; set; }
    public int InstructorCount  { get; set; }
    public int StudentCount     { get; set; }
    public int ActiveCount      { get; set; }
    public int InactiveCount    { get; set; }
    public List<UserDto> RecentUsers { get; set; } = [];

    public async Task OnGetAsync()
    {
        if (!User.IsInRole("Admin")) return;  // Instructors skip stat loading

        TotalUsers      = await _db.Users.CountAsync();
        AdminCount      = await _db.Users.CountAsync(u => u.Role == "Admin");
        InstructorCount = await _db.Users.CountAsync(u => u.Role == "Instructor");
        StudentCount    = await _db.Users.CountAsync(u => u.Role == "User");
        ActiveCount     = await _db.Users.CountAsync(u => u.IsActive);
        InactiveCount   = TotalUsers - ActiveCount;

        RecentUsers = await _db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .Select(u => new UserDto
            {
                Id          = u.Id,
                FirstName   = u.FirstName,
                LastName    = u.LastName,
                FullName    = u.FirstName + " " + u.LastName,
                Email       = u.Email,
                Role        = u.Role,
                IsActive    = u.IsActive,
                CreatedAt   = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync();
    }
}
