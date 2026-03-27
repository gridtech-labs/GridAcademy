using GridAcademy.DTOs.Users;
using GridAcademy.Jobs;
using GridAcademy.Services;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly IUserService _users;
    private readonly IBackgroundJobClient _jobs;

    public CreateModel(IUserService users, IBackgroundJobClient jobs)
    {
        _users = users;
        _jobs  = jobs;
    }

    [BindProperty]
    public CreateUserRequest Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        try
        {
            var user = await _users.CreateAsync(Input);

            // Fire-and-forget welcome email — Hangfire retries on failure
            _jobs.Enqueue<EmailJob>(j => j.SendWelcomeEmailAsync(user.Email, user.FullName));

            TempData["Success"] = $"User '{user.FullName}' was created successfully.";
            return RedirectToPage("./Index");
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}
