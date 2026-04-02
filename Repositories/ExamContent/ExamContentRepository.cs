using GridAcademy.Data;
using GridAcademy.Data.Entities.Exam;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Repositories.ExamContent;

public class ExamContentRepository(AppDbContext db) : IExamContentRepository
{
    public Task AddExamAsync(Exam exam, CancellationToken ct = default) => db.Exams.AddAsync(exam, ct).AsTask();

    public Task<bool> ExamSlugExistsAsync(string slug, CancellationToken ct = default) =>
        db.Exams.AnyAsync(x => x.Slug == slug, ct);

    public IQueryable<Exam> QueryExams() => db.Exams.AsNoTracking();

    public Task<Exam?> GetExamBySlugAsync(string slug, CancellationToken ct = default) =>
        db.Exams.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive, ct);

    public Task AddNotificationAsync(ExamNotification notification, CancellationToken ct = default) =>
        db.ExamNotifications.AddAsync(notification, ct).AsTask();

    public IQueryable<ExamNotification> QueryNotifications() =>
        db.ExamNotifications.AsNoTracking().Include(x => x.Exam);

    public Task<ExamNotification?> GetNotificationBySlugAsync(string slug, CancellationToken ct = default) =>
        db.ExamNotifications.AsNoTracking()
            .Include(x => x.Exam)
            .FirstOrDefaultAsync(x => x.Slug == slug && x.Status == PublicationStatus.Published && (x.Exam == null || x.Exam.IsActive), ct);

    public Task<ExamNotification?> GetNotificationByIdAsync(Guid id, CancellationToken ct = default) =>
        db.ExamNotifications.Include(x => x.Exam).FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task AddVersionAsync(ContentVersion version, CancellationToken ct = default) =>
        db.ContentVersions.AddAsync(version, ct).AsTask();

    public Task<List<ContentVersion>> GetVersionsAsync(string entityType, Guid entityId, CancellationToken ct = default) =>
        db.ContentVersions.AsNoTracking()
            .Where(x => x.EntityType == entityType && x.EntityId == entityId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public Task<bool> HashExistsAsync(string hashValue, CancellationToken ct = default) =>
        db.ContentHashes.AsNoTracking().AnyAsync(x => x.HashValue == hashValue, ct);

    public Task AddHashAsync(ContentHash hash, CancellationToken ct = default) => db.ContentHashes.AddAsync(hash, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
