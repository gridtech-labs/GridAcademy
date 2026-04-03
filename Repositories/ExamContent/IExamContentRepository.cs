using GridAcademy.Data.Entities.Exam;

namespace GridAcademy.Repositories.ExamContent;

public interface IExamContentRepository
{
    Task EnsureExamContentSchemaAsync(CancellationToken ct = default);

    Task AddExamAsync(Exam exam, CancellationToken ct = default);
    Task<bool> ExamSlugExistsAsync(string slug, CancellationToken ct = default);
    IQueryable<Exam> QueryExams();
    Task<Exam?> GetExamBySlugAsync(string slug, CancellationToken ct = default);

    Task AddNotificationAsync(ExamNotification notification, CancellationToken ct = default);
    IQueryable<ExamNotification> QueryNotifications();
    Task<ExamNotification?> GetNotificationBySlugAsync(string slug, CancellationToken ct = default);
    Task<ExamNotification?> GetNotificationByIdAsync(Guid id, CancellationToken ct = default);

    Task AddVersionAsync(ContentVersion version, CancellationToken ct = default);
    Task<List<ContentVersion>> GetVersionsAsync(string entityType, Guid entityId, CancellationToken ct = default);

    Task<bool> HashExistsAsync(string hashValue, CancellationToken ct = default);
    Task AddHashAsync(ContentHash hash, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
