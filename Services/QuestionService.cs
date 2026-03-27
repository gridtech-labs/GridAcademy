using GridAcademy.Common;
using GridAcademy.Data;
using GridAcademy.Data.Entities.Content;
using GridAcademy.DTOs.Content.Masters;
using GridAcademy.DTOs.Content.Questions;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services;

public class QuestionService : IQuestionService
{
    private readonly AppDbContext _db;

    public QuestionService(AppDbContext db) => _db = db;

    // ── List ─────────────────────────────────────────────────────────────────

    public async Task<PagedResult<QuestionDto>> GetQuestionsAsync(QuestionListRequest req)
    {
        var q = _db.Questions
            .Include(x => x.Subject)
            .Include(x => x.Topic)
            .Include(x => x.DifficultyLevel)
            .Include(x => x.ComplexityLevel)
            .Include(x => x.Marks)
            .Include(x => x.NegativeMarks)
            .Include(x => x.ExamType)
            .Include(x => x.Options)
            .Include(x => x.QuestionTags).ThenInclude(qt => qt.Tag)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = req.Search.ToLower();
            q = q.Where(x => x.Text.ToLower().Contains(term));
        }

        if (req.SubjectId.HasValue)        q = q.Where(x => x.SubjectId == req.SubjectId);
        if (req.TopicId.HasValue)          q = q.Where(x => x.TopicId == req.TopicId);
        if (req.DifficultyLevelId.HasValue) q = q.Where(x => x.DifficultyLevelId == req.DifficultyLevelId);
        if (req.ExamTypeId.HasValue)       q = q.Where(x => x.ExamTypeId == req.ExamTypeId);
        if (req.QuestionType.HasValue)     q = q.Where(x => x.QuestionType == req.QuestionType);
        if (req.Status.HasValue)           q = q.Where(x => x.Status == req.Status);

        var totalCount = await q.CountAsync();

        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync();

        return new PagedResult<QuestionDto>
        {
            Items      = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page       = req.Page,
            PageSize   = req.PageSize
        };
    }

    // ── Get Single ───────────────────────────────────────────────────────────

    public async Task<QuestionDto> GetByIdAsync(Guid id)
    {
        var entity = await LoadFullAsync(id) ?? throw new KeyNotFoundException($"Question {id} not found.");
        return MapToDto(entity);
    }

    // ── Create ───────────────────────────────────────────────────────────────

    public async Task<QuestionDto> CreateAsync(CreateQuestionRequest r, Guid? createdBy = null)
    {
        ValidateRequest(r);

        var entity = new Question
        {
            Text             = r.Text.Trim(),
            Solution         = r.Solution?.Trim(),
            Subtopic         = r.Subtopic?.Trim(),
            QuestionType     = r.QuestionType,
            Status           = QuestionStatus.Draft,
            SubjectId        = r.SubjectId,
            TopicId          = r.TopicId,
            DifficultyLevelId = r.DifficultyLevelId,
            ComplexityLevelId = r.ComplexityLevelId,
            MarksId          = r.MarksId,
            NegativeMarksId  = r.NegativeMarksId,
            ExamTypeId       = r.ExamTypeId,
            NumericalAnswer  = r.NumericalAnswer,
            CreatedBy        = createdBy,
            UpdatedBy        = createdBy
        };

        foreach (var opt in r.Options)
            entity.Options.Add(new QuestionOption { Label = opt.Label, Text = opt.Text.Trim(), IsCorrect = opt.IsCorrect });

        foreach (var tagId in r.TagIds.Distinct())
            entity.QuestionTags.Add(new QuestionTag { TagId = tagId });

        _db.Questions.Add(entity);
        await _db.SaveChangesAsync();

        return MapToDto((await LoadFullAsync(entity.Id))!);
    }

    // ── Update ───────────────────────────────────────────────────────────────

    public async Task<QuestionDto> UpdateAsync(Guid id, UpdateQuestionRequest r, Guid? updatedBy = null)
    {
        ValidateRequest(r);

        var entity = await _db.Questions
            .Include(x => x.Options)
            .Include(x => x.QuestionTags)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Question {id} not found.");

        entity.Text              = r.Text.Trim();
        entity.Solution          = r.Solution?.Trim();
        entity.Subtopic          = r.Subtopic?.Trim();
        entity.QuestionType      = r.QuestionType;
        entity.Status            = r.Status;
        entity.SubjectId         = r.SubjectId;
        entity.TopicId           = r.TopicId;
        entity.DifficultyLevelId = r.DifficultyLevelId;
        entity.ComplexityLevelId = r.ComplexityLevelId;
        entity.MarksId           = r.MarksId;
        entity.NegativeMarksId   = r.NegativeMarksId;
        entity.ExamTypeId        = r.ExamTypeId;
        entity.NumericalAnswer   = r.NumericalAnswer;
        entity.UpdatedBy         = updatedBy;

        // Replace options
        _db.QuestionOptions.RemoveRange(entity.Options);
        foreach (var opt in r.Options)
            entity.Options.Add(new QuestionOption { Label = opt.Label, Text = opt.Text.Trim(), IsCorrect = opt.IsCorrect });

        // Replace tags
        _db.QuestionTags.RemoveRange(entity.QuestionTags);
        foreach (var tagId in r.TagIds.Distinct())
            entity.QuestionTags.Add(new QuestionTag { TagId = tagId });

        await _db.SaveChangesAsync();
        return MapToDto((await LoadFullAsync(entity.Id))!);
    }

    // ── Publish / Unpublish ──────────────────────────────────────────────────

    public async Task PublishAsync(Guid id)
    {
        var entity = await _db.Questions.Include(x => x.Options).FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Question {id} not found.");

        // Note: we intentionally do NOT block publishing when no correct option is marked.
        // PDF/OCR imports often miss the answer line; the instructor can fix the correct
        // option via the Edit page after publishing. Scoring will award 0 marks for the
        // question until the correct option is set.

        entity.Status = QuestionStatus.Published;
        await _db.SaveChangesAsync();
    }

    public async Task UnpublishAsync(Guid id)
    {
        var entity = await _db.Questions.FindAsync(id)
            ?? throw new KeyNotFoundException($"Question {id} not found.");
        entity.Status = QuestionStatus.Draft;
        await _db.SaveChangesAsync();
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _db.Questions.FindAsync(id)
            ?? throw new KeyNotFoundException($"Question {id} not found.");
        _db.Questions.Remove(entity);
        await _db.SaveChangesAsync();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private Task<Question?> LoadFullAsync(Guid id) =>
        _db.Questions
            .Include(x => x.Subject)
            .Include(x => x.Topic)
            .Include(x => x.DifficultyLevel)
            .Include(x => x.ComplexityLevel)
            .Include(x => x.Marks)
            .Include(x => x.NegativeMarks)
            .Include(x => x.ExamType)
            .Include(x => x.Options)
            .Include(x => x.QuestionTags).ThenInclude(qt => qt.Tag)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

    private static void ValidateRequest(CreateQuestionRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Text))
            throw new ArgumentException("Question text is required.");

        if (r.QuestionType == QuestionType.NAT)
        {
            if (!r.NumericalAnswer.HasValue)
                throw new ArgumentException("NumericalAnswer is required for Numerical questions.");
        }
        else
        {
            if (r.Options.Count == 0)
                throw new ArgumentException("Options are required for MCQ questions.");
        }
    }

    private static QuestionDto MapToDto(Question q) => new()
    {
        Id               = q.Id,
        Text             = q.Text,
        Solution         = q.Solution,
        QuestionType     = q.QuestionType,
        Status           = q.Status,
        Subtopic         = q.Subtopic,
        SubjectId        = q.SubjectId,
        SubjectName      = q.Subject?.Name ?? "",
        TopicId          = q.TopicId,
        TopicName        = q.Topic?.Name ?? "",
        DifficultyLevelId = q.DifficultyLevelId,
        DifficultyLevel  = q.DifficultyLevel?.Name ?? "",
        ComplexityLevelId = q.ComplexityLevelId,
        ComplexityLevel  = q.ComplexityLevel?.Name ?? "",
        MarksId          = q.MarksId,
        Marks            = q.Marks?.Value ?? 0,
        NegativeMarksId  = q.NegativeMarksId,
        NegativeMarks    = q.NegativeMarks?.Value ?? 0,
        ExamTypeId       = q.ExamTypeId,
        ExamType         = q.ExamType?.Name ?? "",
        NumericalAnswer  = q.NumericalAnswer,
        Options          = q.Options?.Select(o => new QuestionOptionDto
                           { Id = o.Id, Label = o.Label, Text = o.Text, IsCorrect = o.IsCorrect }).ToList() ?? [],
        Tags             = q.QuestionTags?.Select(qt => new TagDto(qt.Tag.Id, qt.Tag.Name, qt.Tag.IsActive, qt.Tag.SortOrder)).ToList() ?? [],
        CreatedAt        = q.CreatedAt,
        UpdatedAt        = q.UpdatedAt
    };
}
