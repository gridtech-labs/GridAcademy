using GridAcademy.Data.Entities.Exam;

namespace GridAcademy.Services.ExamContent.Scraping.Models;

public class ScrapedNotification
{
    public string Title { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public DateTime? PublishedDate { get; set; }
    public ExamNotificationType NotificationType { get; set; } = ExamNotificationType.Notification;
}
