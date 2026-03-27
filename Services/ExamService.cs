using GridAcademy.Data;
using GridAcademy.Data.Entities.Exam;
using GridAcademy.DTOs.Exam;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services;

public class ExamService(AppDbContext db) : IExamService
{
    // ── Mappers ───────────────────────────────────────────────────────────

    private static ExamLevelDto MapLevel(ExamLevel l, int count) =>
        new(l.Id, l.Name, l.IsActive, l.SortOrder, count);

    private static ExamPageCardDto MapCard(ExamPage e) => new(
        e.Id, e.Slug, e.Title, e.ShortDescription, e.ThumbnailUrl,
        e.ExamLevel?.Name, e.ExamType?.Name, e.ConductingBody,
        e.Tests.Count, e.IsFeatured, e.Status, e.CreatedAt);

    private static ExamPageDetailDto MapDetail(ExamPage e) => new(
        e.Id, e.Slug, e.Title, e.ShortDescription,
        e.Overview, e.Eligibility, e.Syllabus, e.ExamPattern,
        e.ImportantDates, e.AdmitCard, e.ResultInfo, e.CutOff, e.HowToApply,
        e.ConductingBody, e.OfficialWebsite, e.NotificationUrl,
        e.ThumbnailUrl, e.BannerUrl,
        e.ExamLevel?.Name, e.ExamType?.Name,
        e.MetaTitle, e.MetaDescription,
        e.IsFeatured, e.ViewCount,
        e.Tests.OrderBy(t => t.SortOrder)
               .Select(t => new ExamTestDto(t.TestId, t.Test?.Title ?? "", t.Test?.Status.ToString() ?? "", t.IsFree, t.SortOrder))
               .ToList(),
        e.UpdatedAt);

    private IQueryable<ExamPage> BaseQuery() =>
        db.ExamPages
          .Include(e => e.ExamLevel)
          .Include(e => e.ExamType)
          .Include(e => e.Tests).ThenInclude(t => t.Test);

    // ── Exam Levels ───────────────────────────────────────────────────────

    public async Task<List<ExamLevelDto>> GetExamLevelsAsync()
    {
        var levels = await db.ExamLevels
            .OrderBy(l => l.SortOrder).ThenBy(l => l.Name)
            .ToListAsync();
        var counts = await db.ExamPages
            .Where(e => e.ExamLevelId != null)
            .GroupBy(e => e.ExamLevelId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id!.Value, x => x.Count);
        return levels.Select(l => MapLevel(l, counts.GetValueOrDefault(l.Id, 0))).ToList();
    }

    public async Task<ExamLevelDto> SaveExamLevelAsync(int? id, SaveExamLevelRequest request)
    {
        ExamLevel entity;
        if (id.HasValue)
        {
            entity = await db.ExamLevels.FindAsync(id.Value)
                ?? throw new KeyNotFoundException($"ExamLevel {id} not found.");
            entity.Name = request.Name; entity.SortOrder = request.SortOrder; entity.IsActive = request.IsActive;
        }
        else
        {
            entity = new ExamLevel { Name = request.Name, SortOrder = request.SortOrder, IsActive = request.IsActive };
            db.ExamLevels.Add(entity);
        }
        await db.SaveChangesAsync();
        return MapLevel(entity, 0);
    }

    public async Task DeleteExamLevelAsync(int id)
    {
        var entity = await db.ExamLevels.FindAsync(id)
            ?? throw new KeyNotFoundException($"ExamLevel {id} not found.");
        db.ExamLevels.Remove(entity);
        await db.SaveChangesAsync();
    }

    // ── Exam Pages ─────────────────────────────────────────────────────────

    public async Task<List<ExamPageCardDto>> GetExamPagesAsync(bool activeOnly = false, int? levelId = null, string? search = null)
    {
        var q = BaseQuery().AsQueryable();
        if (activeOnly) q = q.Where(e => e.IsActive && e.Status == ExamPageStatus.Published);
        if (levelId.HasValue) q = q.Where(e => e.ExamLevelId == levelId.Value);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(e => e.Title.ToLower().Contains(search.ToLower()) ||
                             (e.ConductingBody != null && e.ConductingBody.ToLower().Contains(search.ToLower())));
        return (await q.OrderBy(e => e.SortOrder).ThenBy(e => e.Title).ToListAsync())
               .Select(MapCard).ToList();
    }

    public async Task<ExamPageDetailDto?> GetExamBySlugAsync(string slug, bool incrementView = false)
    {
        var e = await BaseQuery().FirstOrDefaultAsync(x => x.Slug == slug.ToLower());
        if (e == null) return null;
        if (incrementView) { e.ViewCount++; await db.SaveChangesAsync(); }
        return MapDetail(e);
    }

    public async Task<ExamPageDetailDto?> GetExamByIdAsync(Guid id)
    {
        var e = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);
        return e == null ? null : MapDetail(e);
    }

    public async Task<ExamPageCardDto> CreateExamAsync(SaveExamPageRequest request, Guid? createdBy = null)
    {
        var slug = request.Slug.Trim().ToLower();
        if (await db.ExamPages.AnyAsync(e => e.Slug == slug))
            throw new InvalidOperationException($"Slug '{slug}' is already in use.");

        var entity = Apply(new ExamPage { Slug = slug, CreatedBy = createdBy, CreatedAt = DateTime.UtcNow }, request);
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = createdBy;
        db.ExamPages.Add(entity);
        await db.SaveChangesAsync();
        return MapCard(await BaseQuery().FirstAsync(e => e.Id == entity.Id));
    }

    public async Task<ExamPageCardDto> UpdateExamAsync(Guid id, SaveExamPageRequest request, Guid? updatedBy = null)
    {
        var entity = await BaseQuery().FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new KeyNotFoundException($"ExamPage {id} not found.");
        var slug = request.Slug.Trim().ToLower();
        if (await db.ExamPages.AnyAsync(e => e.Slug == slug && e.Id != id))
            throw new InvalidOperationException($"Slug '{slug}' is already in use.");
        Apply(entity, request);
        entity.Slug = slug; entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = updatedBy;
        await db.SaveChangesAsync();
        return MapCard(await BaseQuery().FirstAsync(e => e.Id == entity.Id));
    }

    public async Task DeleteExamAsync(Guid id)
    {
        var entity = await db.ExamPages.FindAsync(id)
            ?? throw new KeyNotFoundException($"ExamPage {id} not found.");
        db.ExamPages.Remove(entity);
        await db.SaveChangesAsync();
    }

    // ── Test mapping ───────────────────────────────────────────────────────

    public async Task MapTestAsync(Guid examId, MapTestRequest request)
    {
        if (!await db.ExamPages.AnyAsync(e => e.Id == examId))
            throw new KeyNotFoundException($"ExamPage {examId} not found.");
        if (await db.ExamPageTests.AnyAsync(t => t.ExamPageId == examId && t.TestId == request.TestId))
            return; // already mapped
        var nextSort = await db.ExamPageTests.Where(t => t.ExamPageId == examId)
                           .MaxAsync(t => (int?)t.SortOrder) ?? 0;
        db.ExamPageTests.Add(new ExamPageTest
        {
            ExamPageId = examId, TestId = request.TestId,
            IsFree = request.IsFree,
            SortOrder = request.SortOrder > 0 ? request.SortOrder : nextSort + 10
        });
        await db.SaveChangesAsync();
    }

    public async Task UnmapTestAsync(Guid examId, Guid testId)
    {
        var entry = await db.ExamPageTests.FindAsync(examId, testId);
        if (entry != null) { db.ExamPageTests.Remove(entry); await db.SaveChangesAsync(); }
    }

    public async Task<List<ExamTestDto>> GetMappedTestsAsync(Guid examId)
    {
        return await db.ExamPageTests
            .Where(t => t.ExamPageId == examId)
            .Include(t => t.Test)
            .OrderBy(t => t.SortOrder)
            .Select(t => new ExamTestDto(t.TestId, t.Test.Title, t.Test.Status.ToString(), t.IsFree, t.SortOrder))
            .ToListAsync();
    }

    // ── Helper ─────────────────────────────────────────────────────────────

    private static ExamPage Apply(ExamPage e, SaveExamPageRequest r)
    {
        e.Title = r.Title; e.ShortDescription = r.ShortDescription;
        e.Overview = r.Overview; e.Eligibility = r.Eligibility;
        e.Syllabus = r.Syllabus; e.ExamPattern = r.ExamPattern;
        e.ImportantDates = r.ImportantDates; e.AdmitCard = r.AdmitCard;
        e.ResultInfo = r.ResultInfo; e.CutOff = r.CutOff; e.HowToApply = r.HowToApply;
        e.ConductingBody = r.ConductingBody; e.OfficialWebsite = r.OfficialWebsite;
        e.NotificationUrl = r.NotificationUrl;
        e.ThumbnailUrl = r.ThumbnailUrl; e.BannerUrl = r.BannerUrl;
        e.ExamLevelId = r.ExamLevelId; e.ExamTypeId = r.ExamTypeId;
        e.IsFeatured = r.IsFeatured; e.IsActive = r.IsActive;
        e.Status = r.Status; e.SortOrder = r.SortOrder;
        e.MetaTitle = r.MetaTitle; e.MetaDescription = r.MetaDescription;
        return e;
    }
}
