using GridAcademy.Data;
using GridAcademy.Data.Entities.Exam;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Repositories.ExamContent;

public class ExamContentRepository(AppDbContext db) : IExamContentRepository
{
    public async Task EnsureExamContentSchemaAsync(CancellationToken ct = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS exams (
                id uuid PRIMARY KEY,
                name character varying(200) NOT NULL,
                slug character varying(220) NOT NULL,
                category character varying(100),
                level character varying(100),
                is_active boolean NOT NULL DEFAULT TRUE,
                created_at timestamptz NOT NULL
            );
            """, ct);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS ix_exams_slug ON exams (slug);
            CREATE INDEX IF NOT EXISTS ix_exams_category_level ON exams (category, level);
            """, ct);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS exam_notifications (
                id uuid PRIMARY KEY,
                exam_id uuid NULL,
                title character varying(300) NOT NULL,
                slug character varying(320) NOT NULL,
                content_html text NOT NULL,
                summary character varying(500),
                notification_type integer NOT NULL,
                important_dates jsonb,
                source_url character varying(500) NOT NULL,
                canonical_url character varying(500),
                meta_title character varying(300),
                meta_description character varying(500),
                status integer NOT NULL DEFAULT 0,
                published_at timestamptz,
                created_at timestamptz NOT NULL,
                updated_at timestamptz NOT NULL
            );
            """, ct);

        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE exam_notifications
                DROP CONSTRAINT IF EXISTS fk_exam_notifications_exams_exam_id;

            ALTER TABLE exam_notifications
                ADD CONSTRAINT fk_exam_notifications_exams_exam_id
                FOREIGN KEY (exam_id) REFERENCES exams(id)
                ON DELETE SET NULL;
            """, ct);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS ix_exam_notifications_slug ON exam_notifications(slug);
            CREATE INDEX IF NOT EXISTS ix_exam_notifications_exam_id ON exam_notifications(exam_id);
            CREATE INDEX IF NOT EXISTS ix_exam_notifications_type ON exam_notifications(notification_type);
            CREATE INDEX IF NOT EXISTS ix_exam_notifications_published_at ON exam_notifications(published_at DESC);
            """, ct);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS content_versions (
                id uuid PRIMARY KEY,
                entity_type character varying(80) NOT NULL,
                entity_id uuid NOT NULL,
                content_html text NOT NULL,
                created_at timestamptz NOT NULL
            );
            """, ct);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS ix_content_versions_entity
            ON content_versions (entity_type, entity_id, created_at DESC);
            """, ct);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS content_hashes (
                id uuid PRIMARY KEY,
                hash_value character varying(64) NOT NULL,
                source_url character varying(500) NOT NULL,
                created_at timestamptz NOT NULL
            );
            """, ct);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS ix_content_hashes_hash_value
            ON content_hashes (hash_value);
            """, ct);
    }

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
