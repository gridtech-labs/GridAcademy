namespace GridAcademy.Services;

public interface IEmailService
{
    Task SendContactFormEmailAsync(string senderName, string senderEmail, string phone, string subject, string message);
    Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody);
}
