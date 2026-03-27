namespace GridAcademy.Jobs;

/// <summary>
/// Background email jobs — enqueued via Hangfire so they are
/// retried automatically on failure without blocking the API response.
///
/// Usage (from a controller or service):
///   _jobs.Enqueue&lt;EmailJob&gt;(j => j.SendWelcomeEmailAsync(email, name));
/// </summary>
public class EmailJob
{
    private readonly ILogger<EmailJob> _logger;

    // In a real project, inject IEmailService (SendGrid, SMTP, etc.) here.
    public EmailJob(ILogger<EmailJob> logger) => _logger = logger;

    /// <summary>Sends a welcome email to a new user.</summary>
    public Task SendWelcomeEmailAsync(string email, string fullName)
    {
        // TODO: replace with real email provider (SendGrid, Mailgun, SMTP, etc.)
        _logger.LogInformation(
            "[EmailJob] Sending welcome email to {FullName} <{Email}>", fullName, email);

        // Simulate async email send
        return Task.CompletedTask;
    }

    /// <summary>Sends a generic notification email.</summary>
    public Task SendNotificationAsync(string email, string subject, string body)
    {
        _logger.LogInformation(
            "[EmailJob] Sending notification to {Email} | Subject: {Subject}", email, subject);

        return Task.CompletedTask;
    }
}
