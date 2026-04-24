using System.Net;
using System.Net.Mail;

namespace GridAcademy.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendContactFormEmailAsync(
        string senderName, string senderEmail, string phone, string subject, string message)
    {
        var contactEmail = _config["Email:ContactEmail"] ?? "info@gridacademy.in";

        var htmlBody = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;background:#f9f9f9;padding:20px;border-radius:8px;">
              <div style="background:#1a3a6b;padding:20px;border-radius:8px 8px 0 0;text-align:center;">
                <h2 style="color:#fff;margin:0;">New Contact Form Submission</h2>
                <p style="color:#a8c4e8;margin:5px 0 0;">GridAcademy — info@gridacademy.in</p>
              </div>
              <div style="background:#fff;padding:24px;border-radius:0 0 8px 8px;border:1px solid #e0e0e0;border-top:none;">
                <table style="width:100%;border-collapse:collapse;">
                  <tr><td style="padding:8px 0;color:#666;font-size:14px;width:120px;"><strong>Name:</strong></td><td style="padding:8px 0;color:#333;">{System.Net.WebUtility.HtmlEncode(senderName)}</td></tr>
                  <tr><td style="padding:8px 0;color:#666;font-size:14px;"><strong>Email:</strong></td><td style="padding:8px 0;"><a href="mailto:{System.Net.WebUtility.HtmlEncode(senderEmail)}" style="color:#1a3a6b;">{System.Net.WebUtility.HtmlEncode(senderEmail)}</a></td></tr>
                  <tr><td style="padding:8px 0;color:#666;font-size:14px;"><strong>Phone:</strong></td><td style="padding:8px 0;color:#333;">{System.Net.WebUtility.HtmlEncode(phone ?? "—")}</td></tr>
                  <tr><td style="padding:8px 0;color:#666;font-size:14px;"><strong>Subject:</strong></td><td style="padding:8px 0;color:#333;font-weight:600;">{System.Net.WebUtility.HtmlEncode(subject)}</td></tr>
                </table>
                <hr style="border:none;border-top:1px solid #eee;margin:16px 0;" />
                <p style="color:#666;font-size:14px;margin:0 0 8px;"><strong>Message:</strong></p>
                <div style="background:#f5f7fa;border-left:4px solid #1a3a6b;padding:16px;border-radius:4px;color:#333;font-size:15px;white-space:pre-wrap;">{System.Net.WebUtility.HtmlEncode(message)}</div>
                <hr style="border:none;border-top:1px solid #eee;margin:20px 0 16px;" />
                <p style="font-size:12px;color:#999;margin:0;">This message was sent via the contact form at <a href="https://www.gridacademy.in/contact" style="color:#1a3a6b;">gridacademy.in/contact</a></p>
              </div>
            </div>
            """;

        await SendEmailAsync(contactEmail, "GridAcademy Team", $"[Contact Form] {subject}", htmlBody);
    }

    public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var host     = _config["Email:SmtpHost"]    ?? "smtp.gmail.com";
        var port     = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var ssl      = bool.Parse(_config["Email:EnableSsl"] ?? "true");
        var from     = _config["Email:FromAddress"] ?? "info@gridacademy.in";
        var fromName = _config["Email:FromName"]    ?? "GridAcademy";
        var user     = _config["Email:Username"]    ?? from;
        var pass     = _config["Email:Password"]    ?? "";

        // Allow Railway env vars to override: Email__Password, etc.
        pass = Environment.GetEnvironmentVariable("Email__Password") ?? pass;

        if (string.IsNullOrWhiteSpace(pass) || pass.StartsWith("REPLACE_"))
        {
            _logger.LogWarning("[Email] SMTP password not configured. Email to {To} was NOT sent. Set Email:Password in appsettings or Email__Password env var.", toEmail);
            return;
        }

        using var smtp    = new SmtpClient(host, port) { EnableSsl = ssl, Credentials = new NetworkCredential(user, pass) };
        using var message = new MailMessage
        {
            From       = new MailAddress(from, fromName),
            Subject    = subject,
            Body       = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(toEmail, toName));

        try
        {
            await smtp.SendMailAsync(message);
            _logger.LogInformation("[Email] Sent to {To}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] Failed to send to {To}: {Subject}", toEmail, subject);
            throw;
        }
    }
}
