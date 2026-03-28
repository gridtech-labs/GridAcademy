using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GridAcademy.Data;
using GridAcademy.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Pages.Account;

public class LoginModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(AppDbContext db, ILogger<LoginModel> logger)
    {
        _db     = db;
        _logger = logger;
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        // Already logged in → go to the appropriate dashboard
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("User"))
                return Redirect("/Student/Dashboard");
            if (User.IsInRole("Provider"))
                return RedirectToPage("/Provider/Dashboard");
            return RedirectToPage("/Admin/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // 1. Find user by email (case-insensitive)
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == Input.Email.ToLower().Trim());

        // 2. Validate credentials — same message for both cases (no user enumeration)
        if (user is null || !PasswordHelper.Verify(Input.Password, user.PasswordHash))
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        // 3. Check account is active
        if (!user.IsActive)
        {
            ErrorMessage = "Your account has been deactivated. Contact an administrator.";
            return Page();
        }

        // 4. Build cookie claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name,           user.FullName),
            new(ClaimTypes.Email,          user.Email),
            new(ClaimTypes.Role,           user.Role)
        };

        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        // 6. Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Login: {Email} ({Role})", user.Email, user.Role);

        // Route to appropriate portal by role
        if (user.Role == "User")
            return Redirect("/Student/Dashboard");

        if (user.Role == "Provider")
            return RedirectToPage("/Provider/Dashboard");

        return RedirectToPage("/Admin/Index");
    }
}

public class LoginInput
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}
