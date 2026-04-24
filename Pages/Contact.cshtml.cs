using System.ComponentModel.DataAnnotations;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages;

[AllowAnonymous]
public class ContactModel : PageModel
{
    private readonly IEmailService _email;
    private readonly ILogger<ContactModel> _logger;

    public ContactModel(IEmailService email, ILogger<ContactModel> logger)
    {
        _email  = email;
        _logger = logger;
    }

    [BindProperty]
    public ContactInputModel Input { get; set; } = new();

    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage   { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            await _email.SendContactFormEmailAsync(
                Input.Name,
                Input.Email,
                Input.Phone ?? "",
                Input.Subject,
                Input.Message);

            SuccessMessage = $"Thank you, {Input.Name}! Your message has been sent. We'll reply to {Input.Email} within 24 hours.";
            Input = new ContactInputModel();
            ModelState.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Contact] Failed to send contact form email from {Email}", Input.Email);
            ErrorMessage = "Sorry, we couldn't send your message right now. Please email us directly at info@gridacademy.in";
        }

        return Page();
    }

    public class ContactInputModel
    {
        [Required(ErrorMessage = "Please enter your name.")]
        [MaxLength(100)]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(200)]
        public string Email { get; set; } = "";

        [Phone]
        [MaxLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Please select a subject.")]
        [MaxLength(100)]
        public string Subject { get; set; } = "";

        [Required(ErrorMessage = "Please enter your message.")]
        [MinLength(10, ErrorMessage = "Message must be at least 10 characters.")]
        [MaxLength(2000)]
        public string Message { get; set; } = "";
    }
}
