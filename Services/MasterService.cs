using GridAcademy.Data;
using GridAcademy.Data.Entities.Content;
using GridAcademy.DTOs.Content.Masters;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services;

public class MasterService : IMasterService
{
    private readonly AppDbContext _db;

    public MasterService(AppDbContext db) => _db = db;

    // ── Question Types ────────────────────────────────────────────────────────

    public async Task<List<QuestionTypeDto>> GetQuestionTypesAsync(bool activeOnly = false)
    {
        var q = _db.QuestionTypes.AsNoTracking();
        if (activeOnly) q = q.Where(x => x.IsActive);
        return await q.OrderBy(x => x.SortOrder)
            .Select(x => new QuestionTypeDto((int)x.Id, x.Name, x.Code, x.Description, x.IsActive, x.SortOrder))
            .ToListAsync();
    }

    public async Task<QuestionTypeDto> UpdateQuestionTypeAsync(int id, string name, string? description, bool isActive)
    {
        var key = (QuestionType)id;
        var e = await _db.QuestionTypes.FindAsync(key)
            ?? throw new KeyNotFoundException($"QuestionType {id} not found.");
        e.Name        = name.Trim();
        e.Description = description?.Trim();
        e.IsActive    = isActive;
        await _db.SaveChangesAsync();
        return new QuestionTypeDto((int)e.Id, e.Name, e.Code, e.Description, e.IsActive, e.SortOrder);
    }

    // ── Subjects ─────────────────────────────────────────────────────────────

    public async Task<List<SubjectDto>> GetSubjectsAsync(bool activeOnly = true)
    {
        var q = _db.Subjects.AsNoTracking();
        if (activeOnly) q = q.Where(x => x.IsActive);
        return await q.OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new SubjectDto(x.Id, x.Name, x.IsActive, x.SortOrder))
            .ToListAsync();
    }

    public async Task<SubjectDto> GetSubjectAsync(int id)
    {
        var e = await _db.Subjects.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Subject {id} not found.");
        return new SubjectDto(e.Id, e.Name, e.IsActive, e.SortOrder);
    }

    public async Task<SubjectDto> CreateSubjectAsync(CreateMasterRequest r)
    {
        var e = new Subject { Name = r.Name.Trim(), IsActive = r.IsActive, SortOrder = r.SortOrder };
        _db.Subjects.Add(e);
        await _db.SaveChangesAsync();
        return new SubjectDto(e.Id, e.Name, e.IsActive, e.SortOrder);
    }

    public async Task<SubjectDto> UpdateSubjectAsync(int id, CreateMasterRequest r)
    {
        var e = await _db.Subjects.FindAsync(id) ?? throw new KeyNotFoundException($"Subject {id} not found.");
        e.Name = r.Name.Trim(); e.IsActive = r.IsActive; e.SortOrder = r.SortOrder;
        await _db.SaveChangesAsync();
        return new SubjectDto(e.Id, e.Name, e.IsActive, e.SortOrder);
    }

    public async Task DeleteSubjectAsync(int id)
    {
        var e = await _db.Subjects.FindAsync(id) ?? throw new KeyNotFoundException($"Subject {id} not found.");
        _db.Subjects.Remove(e);
        await _db.SaveChangesAsync();
    }

    // ── Topics ───────────────────────────────────────────────────────────────

    public async Task<List<TopicDto>> GetTopicsAsync(int? subjectId = null, bool activeOnly = true)
    {
        var q = _db.Topics.Include(x => x.Subject).AsNoTracking();
        if (activeOnly) q = q.Where(x => x.IsActive);
        if (subjectId.HasValue) q = q.Where(x => x.SubjectId == subjectId);
        return await q.OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new TopicDto(x.Id, x.Name, x.IsActive, x.SortOrder, x.SubjectId, x.Subject.Name))
            .ToListAsync();
    }

    public async Task<TopicDto> GetTopicAsync(int id)
    {
        var e = await _db.Topics.Include(x => x.Subject).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Topic {id} not found.");
        return new TopicDto(e.Id, e.Name, e.IsActive, e.SortOrder, e.SubjectId, e.Subject.Name);
    }

    public async Task<TopicDto> CreateTopicAsync(CreateTopicRequest r)
    {
        if (!await _db.Subjects.AnyAsync(x => x.Id == r.SubjectId))
            throw new KeyNotFoundException($"Subject {r.SubjectId} not found.");
        var e = new Topic { Name = r.Name.Trim(), SubjectId = r.SubjectId, IsActive = r.IsActive, SortOrder = r.SortOrder };
        _db.Topics.Add(e);
        await _db.SaveChangesAsync();
        var subjectName = (await _db.Subjects.AsNoTracking().FirstAsync(x => x.Id == r.SubjectId)).Name;
        return new TopicDto(e.Id, e.Name, e.IsActive, e.SortOrder, e.SubjectId, subjectName);
    }

    public async Task<TopicDto> UpdateTopicAsync(int id, CreateTopicRequest r)
    {
        var e = await _db.Topics.FindAsync(id) ?? throw new KeyNotFoundException($"Topic {id} not found.");
        e.Name = r.Name.Trim(); e.SubjectId = r.SubjectId; e.IsActive = r.IsActive; e.SortOrder = r.SortOrder;
        await _db.SaveChangesAsync();
        var subjectName = (await _db.Subjects.AsNoTracking().FirstAsync(x => x.Id == r.SubjectId)).Name;
        return new TopicDto(e.Id, e.Name, e.IsActive, e.SortOrder, e.SubjectId, subjectName);
    }

    public async Task DeleteTopicAsync(int id)
    {
        var e = await _db.Topics.FindAsync(id) ?? throw new KeyNotFoundException($"Topic {id} not found.");
        _db.Topics.Remove(e);
        await _db.SaveChangesAsync();
    }

    // ── Generic helpers ──────────────────────────────────────────────────────

    public async Task<List<DifficultyLevelDto>> GetDifficultyLevelsAsync() =>
        await _db.DifficultyLevels.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder)
            .Select(x => new DifficultyLevelDto(x.Id, x.Name, x.IsActive, x.SortOrder)).ToListAsync();

    public async Task<DifficultyLevelDto> CreateDifficultyLevelAsync(CreateMasterRequest r)
    {
        var e = new DifficultyLevel { Name = r.Name.Trim(), IsActive = r.IsActive, SortOrder = r.SortOrder };
        _db.DifficultyLevels.Add(e);
        await _db.SaveChangesAsync();
        return new DifficultyLevelDto(e.Id, e.Name, e.IsActive, e.SortOrder);
    }

    public async Task<List<ComplexityLevelDto>> GetComplexityLevelsAsync() =>
        await _db.ComplexityLevels.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder)
            .Select(x => new ComplexityLevelDto(x.Id, x.Name, x.IsActive, x.SortOrder)).ToListAsync();

    public async Task<ComplexityLevelDto> CreateComplexityLevelAsync(CreateMasterRequest r)
    {
        var e = new ComplexityLevel { Name = r.Name.Trim(), IsActive = r.IsActive, SortOrder = r.SortOrder };
        _db.ComplexityLevels.Add(e);
        await _db.SaveChangesAsync();
        return new ComplexityLevelDto(e.Id, e.Name, e.IsActive, e.SortOrder);
    }

    public async Task<List<ExamTypeDto>> GetExamTypesAsync() =>
        await _db.ExamTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder)
            .Select(x => new ExamTypeDto(x.Id, x.Name, x.IsActive, x.SortOrder)).ToListAsync();

    public async Task<ExamTypeDto> CreateExamTypeAsync(CreateMasterRequest r)
    {
        var e = new ExamType { Name = r.Name.Trim(), IsActive = r.IsActive, SortOrder = r.SortOrder };
        _db.ExamTypes.Add(e);
        await _db.SaveChangesAsync();
        return new ExamTypeDto(e.Id, e.Name, e.IsActive, e.SortOrder);
    }

    public async Task<List<TagDto>> GetTagsAsync() =>
        await _db.Tags.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new TagDto(x.Id, x.Name, x.IsActive, x.SortOrder)).ToListAsync();

    public async Task<TagDto> CreateTagAsync(CreateMasterRequest r)
    {
        var e = new Tag { Name = r.Name.Trim(), IsActive = r.IsActive, SortOrder = r.SortOrder };
        _db.Tags.Add(e);
        await _db.SaveChangesAsync();
        return new TagDto(e.Id, e.Name, e.IsActive, e.SortOrder);
    }

    public async Task<List<MarksDto>> GetMarksAsync() =>
        await _db.MarksMaster.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Value)
            .Select(x => new MarksDto(x.Id, x.Name, x.IsActive, x.SortOrder, x.Value)).ToListAsync();

    public async Task<MarksDto> CreateMarksAsync(CreateMarksRequest r)
    {
        var e = new MarksMaster { Name = r.Name.Trim(), Value = r.Value, IsActive = r.IsActive, SortOrder = r.SortOrder };
        _db.MarksMaster.Add(e);
        await _db.SaveChangesAsync();
        return new MarksDto(e.Id, e.Name, e.IsActive, e.SortOrder, e.Value);
    }

    public async Task<List<NegativeMarksDto>> GetNegativeMarksAsync() =>
        await _db.NegativeMarksMaster.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Value)
            .Select(x => new NegativeMarksDto(x.Id, x.Name, x.IsActive, x.SortOrder, x.Value)).ToListAsync();

    public async Task<NegativeMarksDto> CreateNegativeMarksAsync(CreateMarksRequest r)
    {
        var e = new NegativeMarksMaster { Name = r.Name.Trim(), Value = r.Value, IsActive = r.IsActive, SortOrder = r.SortOrder };
        _db.NegativeMarksMaster.Add(e);
        await _db.SaveChangesAsync();
        return new NegativeMarksDto(e.Id, e.Name, e.IsActive, e.SortOrder, e.Value);
    }

    public async Task DeleteDifficultyLevelAsync(int id)
    {
        var e = await _db.DifficultyLevels.FindAsync(id) ?? throw new KeyNotFoundException($"DifficultyLevel {id} not found.");
        _db.DifficultyLevels.Remove(e);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteComplexityLevelAsync(int id)
    {
        var e = await _db.ComplexityLevels.FindAsync(id) ?? throw new KeyNotFoundException($"ComplexityLevel {id} not found.");
        _db.ComplexityLevels.Remove(e);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteExamTypeAsync(int id)
    {
        var e = await _db.ExamTypes.FindAsync(id) ?? throw new KeyNotFoundException($"ExamType {id} not found.");
        _db.ExamTypes.Remove(e);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteTagAsync(int id)
    {
        var e = await _db.Tags.FindAsync(id) ?? throw new KeyNotFoundException($"Tag {id} not found.");
        _db.Tags.Remove(e);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteMarksAsync(int id)
    {
        var e = await _db.MarksMaster.FindAsync(id) ?? throw new KeyNotFoundException($"Marks {id} not found.");
        _db.MarksMaster.Remove(e);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteNegativeMarksAsync(int id)
    {
        var e = await _db.NegativeMarksMaster.FindAsync(id) ?? throw new KeyNotFoundException($"NegativeMarks {id} not found.");
        _db.NegativeMarksMaster.Remove(e);
        await _db.SaveChangesAsync();
    }
}
