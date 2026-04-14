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

        if (req.SubjectId.HasValue)         q = q.Where(x => x.SubjectId         == req.SubjectId);
        if (req.TopicId.HasValue)           q = q.Where(x => x.TopicId           == req.TopicId);
        if (req.DifficultyLevelId.HasValue) q = q.Where(x => x.DifficultyLevelId == req.DifficultyLevelId);
        if (req.ExamTypeId.HasValue)        q = q.Where(x => x.ExamTypeId        == req.ExamTypeId);
        if (req.QuestionType.HasValue)      q = q.Where(x => x.QuestionType      == req.QuestionType);
        if (req.Status.HasValue)            q = q.Where(x => x.Status            == req.Status);
        if (req.TestId.HasValue)
        {
            var mappedIds = await _db.TestQuestions
                .Where(tq => tq.TestId == req.TestId.Value)
                .Select(tq => tq.QuestionId)
                .ToListAsync();
            q = q.Where(x => mappedIds.Contains(x.Id));
        }

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

        // Passage-based is handled specially — creates passage + N sub-questions
        if (r.QuestionType == QuestionType.PassageBased)
            return await CreatePassageSetAsync(r, createdBy);

        var entity = BuildBaseEntity(r, createdBy);
        AttachTypeData(entity, r);

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
            .Include(x => x.Blanks)
            .Include(x => x.MatchItems)
            .Include(x => x.MatchCorrect)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Question {id} not found.");

        // ── Core fields ───────────────────────────────────────────────────────
        entity.Text               = r.Text.Trim();
        entity.Solution           = r.Solution?.Trim();
        entity.Subtopic           = r.Subtopic?.Trim();
        entity.QuestionType       = r.QuestionType;
        entity.Status             = r.Status;
        entity.SubjectId          = r.SubjectId;
        entity.TopicId            = r.TopicId;
        entity.DifficultyLevelId  = r.DifficultyLevelId;
        entity.ComplexityLevelId  = r.ComplexityLevelId;
        entity.MarksId            = r.MarksId;
        entity.NegativeMarksId    = r.NegativeMarksId;
        entity.ExamTypeId         = r.ExamTypeId;
        entity.NumericalAnswer    = r.NumericalAnswer;
        entity.NumericalTolerance = r.NumericalTolerance;
        entity.AssertionText      = r.AssertionText?.Trim();
        entity.ReasonText         = r.ReasonText?.Trim();
        entity.UpdatedBy          = updatedBy;
        entity.UpdatedAt          = DateTime.UtcNow;

        // For PassageBased: also update the passage text/title if provided
        if (r.QuestionType == QuestionType.PassageBased && entity.PassageId.HasValue
            && !string.IsNullOrWhiteSpace(r.PassageText))
        {
            var passage = await _db.QuestionPassages.FindAsync(entity.PassageId.Value);
            if (passage != null)
            {
                passage.PassageText = r.PassageText.Trim();
                if (!string.IsNullOrWhiteSpace(r.PassageTitle))
                    passage.Title = r.PassageTitle.Trim();
                passage.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Replace all child collections
        _db.QuestionOptions.RemoveRange(entity.Options);
        _db.QuestionBlanks.RemoveRange(entity.Blanks);
        _db.QuestionMatchItems.RemoveRange(entity.MatchItems);
        _db.QuestionMatchCorrects.RemoveRange(entity.MatchCorrect);
        _db.QuestionTags.RemoveRange(entity.QuestionTags);

        AttachTypeData(entity, r);

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
        // option via the Edit page after publishing.
        entity.Status = QuestionStatus.Published;
        await _db.SaveChangesAsync();
    }

    public async Task UnpublishAsync(Guid id)
    {
        var entity = await _db.Questions.FindAsync(id)
            ?? throw new KeyNotFoundException($"Question {id} not found.");
        entity.Status = QuestionStatus.Published;
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

    // ── Private: build base entity ───────────────────────────────────────────

    private static Question BuildBaseEntity(CreateQuestionRequest r, Guid? createdBy) => new()
    {
        Text               = r.Text.Trim(),
        Solution           = r.Solution?.Trim(),
        Subtopic           = r.Subtopic?.Trim(),
        QuestionType       = r.QuestionType,
        Status             = r.Status,
        SubjectId          = r.SubjectId,
        TopicId            = r.TopicId,
        DifficultyLevelId  = r.DifficultyLevelId,
        ComplexityLevelId  = r.ComplexityLevelId,
        MarksId            = r.MarksId,
        NegativeMarksId    = r.NegativeMarksId,
        ExamTypeId         = r.ExamTypeId,
        NumericalAnswer    = r.NumericalAnswer,
        NumericalTolerance = r.NumericalTolerance,
        AssertionText      = r.AssertionText?.Trim(),
        ReasonText         = r.ReasonText?.Trim(),
        CreatedBy          = createdBy,
        UpdatedBy          = createdBy
    };

    // ── Private: attach type-specific child data ─────────────────────────────

    private static void AttachTypeData(Question entity, CreateQuestionRequest r)
    {
        switch (r.QuestionType)
        {
            case QuestionType.MCQ:
            case QuestionType.MSQ:
            case QuestionType.TrueFalse:
            case QuestionType.AssertionReason:
            case QuestionType.PassageBased:   // sub-question options
                foreach (var opt in r.Options)
                    entity.Options.Add(new QuestionOption
                        { Label = opt.Label, Text = opt.Text.Trim(), IsCorrect = opt.IsCorrect });
                break;

            case QuestionType.FillInBlanks:
                foreach (var b in r.Blanks)
                    entity.Blanks.Add(new QuestionBlank
                    {
                        BlankIndex       = b.BlankIndex,
                        CorrectAnswer    = b.CorrectAnswer.Trim(),
                        AlternateAnswers = b.AlternateAnswers?.Trim(),
                        CaseSensitive    = b.CaseSensitive
                    });
                break;

            case QuestionType.MatchTheFollowing:
            case QuestionType.MatrixMatch:
                int sort = 0;
                foreach (var item in r.MatchItems)
                    entity.MatchItems.Add(new QuestionMatchItem
                    {
                        ColumnSide = item.ColumnSide,
                        Label      = item.Label,
                        Text       = item.Text.Trim(),
                        SortOrder  = sort++
                    });
                foreach (var mc in r.MatchCorrect)
                    entity.MatchCorrect.Add(new QuestionMatchCorrect
                        { LeftLabel = mc.LeftLabel, RightLabel = mc.RightLabel });
                break;

            case QuestionType.NAT:
                break; // NumericalAnswer already set on the entity
        }

        foreach (var tagId in r.TagIds.Distinct())
            entity.QuestionTags.Add(new QuestionTag { TagId = tagId });
    }

    // ── Private: create passage + sub-questions ───────────────────────────────

    private async Task<QuestionDto> CreatePassageSetAsync(CreateQuestionRequest r, Guid? createdBy)
    {
        if (string.IsNullOrWhiteSpace(r.PassageText))
            throw new ArgumentException("Passage text is required for Passage Based questions.");
        if (r.SubQuestions.Count == 0)
            throw new ArgumentException("At least one sub-question is required for Passage Based questions.");

        // Create or reuse the passage
        QuestionPassage passage;
        if (r.PassageId.HasValue)
        {
            passage = await _db.QuestionPassages.FindAsync(r.PassageId.Value)
                ?? throw new ArgumentException("Referenced passage not found.");
        }
        else
        {
            passage = new QuestionPassage
            {
                Title       = r.PassageTitle?.Trim() ?? "",
                PassageText = r.PassageText.Trim()
            };
            _db.QuestionPassages.Add(passage);
            await _db.SaveChangesAsync();
        }

        // Create each sub-question
        Guid firstId = Guid.Empty;
        foreach (var sq in r.SubQuestions)
        {
            if (string.IsNullOrWhiteSpace(sq.Text)) continue;

            var subEntity = new Question
            {
                Text              = sq.Text.Trim(),
                Solution          = sq.Solution?.Trim(),
                QuestionType      = QuestionType.PassageBased,
                Status            = r.Status,
                SubjectId         = r.SubjectId,
                TopicId           = r.TopicId,
                DifficultyLevelId = r.DifficultyLevelId,
                ComplexityLevelId = r.ComplexityLevelId,
                MarksId           = r.MarksId,
                NegativeMarksId   = r.NegativeMarksId,
                ExamTypeId        = r.ExamTypeId,
                PassageId         = passage.Id,
                CreatedBy         = createdBy,
                UpdatedBy         = createdBy
            };
            foreach (var opt in sq.Options)
                subEntity.Options.Add(new QuestionOption
                    { Label = opt.Label, Text = opt.Text.Trim(), IsCorrect = opt.IsCorrect });
            foreach (var tagId in r.TagIds.Distinct())
                subEntity.QuestionTags.Add(new QuestionTag { TagId = tagId });

            _db.Questions.Add(subEntity);
            if (firstId == Guid.Empty) firstId = subEntity.Id;
        }
        await _db.SaveChangesAsync();
        return MapToDto((await LoadFullAsync(firstId))!);
    }

    // ── Private: load full question ───────────────────────────────────────────

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
            .Include(x => x.Blanks)
            .Include(x => x.MatchItems)
            .Include(x => x.MatchCorrect)
            .Include(x => x.Passage)
            .Include(x => x.QuestionTags).ThenInclude(qt => qt.Tag)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

    // ── Private: validate request ─────────────────────────────────────────────

    private static void ValidateRequest(CreateQuestionRequest r)
    {
        switch (r.QuestionType)
        {
            case QuestionType.PassageBased:
                if (r.SubQuestions.Count == 0)
                    throw new ArgumentException("At least one sub-question is required for Passage Based questions.");
                foreach (var sq in r.SubQuestions)
                {
                    if (string.IsNullOrWhiteSpace(sq.Text))
                        throw new ArgumentException("Each sub-question must have a question text.");
                    if (sq.Options.Count == 0)
                        throw new ArgumentException("Each sub-question must have at least one option.");
                }
                break;

            case QuestionType.NAT:
                if (string.IsNullOrWhiteSpace(r.Text))
                    throw new ArgumentException("Question text is required.");
                if (!r.NumericalAnswer.HasValue)
                    throw new ArgumentException("NumericalAnswer is required for NAT questions.");
                break;

            case QuestionType.MCQ:
            case QuestionType.MSQ:
                if (string.IsNullOrWhiteSpace(r.Text))
                    throw new ArgumentException("Question text is required.");
                if (r.Options.Count == 0)
                    throw new ArgumentException("At least one option is required for MCQ/MSQ questions.");
                break;

            case QuestionType.TrueFalse:
                if (string.IsNullOrWhiteSpace(r.Text))
                    throw new ArgumentException("Question text (statement) is required for True/False questions.");
                if (r.Options.Count != 2)
                    throw new ArgumentException("True/False questions must have exactly 2 options (T and F).");
                break;

            case QuestionType.FillInBlanks:
                if (string.IsNullOrWhiteSpace(r.Text))
                    throw new ArgumentException("Question text is required (use [BLANK_1] markers).");
                if (r.Blanks.Count == 0)
                    throw new ArgumentException("At least one blank answer is required for Fill in the Blanks questions.");
                break;

            case QuestionType.MatchTheFollowing:
            case QuestionType.MatrixMatch:
                if (string.IsNullOrWhiteSpace(r.Text))
                    throw new ArgumentException("Question text / instruction is required.");
                if (r.MatchItems.Count == 0)
                    throw new ArgumentException("At least one match item is required.");
                if (r.MatchCorrect.Count == 0)
                    throw new ArgumentException("At least one correct matching pair is required.");
                break;

            case QuestionType.AssertionReason:
                if (string.IsNullOrWhiteSpace(r.AssertionText))
                    throw new ArgumentException("Assertion (A) text is required.");
                if (string.IsNullOrWhiteSpace(r.ReasonText))
                    throw new ArgumentException("Reason (R) text is required.");
                break;

            default:
                if (string.IsNullOrWhiteSpace(r.Text))
                    throw new ArgumentException("Question text is required.");
                break;
        }
    }

    // ── Private: map entity → DTO ─────────────────────────────────────────────

    private static QuestionDto MapToDto(Question q) => new()
    {
        Id                = q.Id,
        Text              = q.Text,
        Solution          = q.Solution,
        QuestionType      = q.QuestionType,
        Status            = q.Status,
        Subtopic          = q.Subtopic,
        SubjectId         = q.SubjectId,
        SubjectName       = q.Subject?.Name ?? "",
        TopicId           = q.TopicId,
        TopicName         = q.Topic?.Name ?? "",
        DifficultyLevelId = q.DifficultyLevelId,
        DifficultyLevel   = q.DifficultyLevel?.Name ?? "",
        ComplexityLevelId = q.ComplexityLevelId,
        ComplexityLevel   = q.ComplexityLevel?.Name ?? "",
        MarksId           = q.MarksId,
        Marks             = q.Marks?.Value ?? 0,
        NegativeMarksId   = q.NegativeMarksId,
        NegativeMarks     = q.NegativeMarks?.Value ?? 0,
        ExamTypeId        = q.ExamTypeId,
        ExamType          = q.ExamType?.Name ?? "",
        NumericalAnswer   = q.NumericalAnswer,
        NumericalTolerance= q.NumericalTolerance,
        AssertionText     = q.AssertionText,
        ReasonText        = q.ReasonText,
        Options           = q.Options?
                              .OrderBy(o => o.SortOrder).ThenBy(o => o.Label)
                              .Select(o => new QuestionOptionDto
                                  { Id = o.Id, Label = o.Label, Text = o.Text, IsCorrect = o.IsCorrect })
                              .ToList() ?? [],
        Blanks            = q.Blanks?
                              .OrderBy(b => b.BlankIndex)
                              .Select(b => new QuestionBlankDto
                              {
                                  BlankIndex       = b.BlankIndex,
                                  CorrectAnswer    = b.CorrectAnswer,
                                  AlternateAnswers = b.AlternateAnswers,
                                  CaseSensitive    = b.CaseSensitive
                              }).ToList() ?? [],
        MatchItems        = q.MatchItems?
                              .OrderBy(i => i.SortOrder)
                              .Select(i => new QuestionMatchItemDto
                              {
                                  ColumnSide = i.ColumnSide,
                                  Label      = i.Label,
                                  Text       = i.Text,
                                  SortOrder  = i.SortOrder
                              }).ToList() ?? [],
        MatchCorrect      = q.MatchCorrect?
                              .Select(c => new QuestionMatchCorrectDto
                                  { LeftLabel = c.LeftLabel, RightLabel = c.RightLabel })
                              .ToList() ?? [],
        Passage           = q.Passage == null ? null : new QuestionPassageDto
                            {
                                Id          = q.Passage.Id,
                                Title       = q.Passage.Title,
                                PassageText = q.Passage.PassageText
                            },
        Tags              = q.QuestionTags?
                              .Select(qt => new TagDto(qt.Tag.Id, qt.Tag.Name, qt.Tag.IsActive, qt.Tag.SortOrder))
                              .ToList() ?? [],
        CreatedAt         = q.CreatedAt,
        UpdatedAt         = q.UpdatedAt
    };
}
