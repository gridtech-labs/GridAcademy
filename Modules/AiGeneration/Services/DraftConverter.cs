using GridAcademy.Data;
using GridAcademy.Data.Entities.Content;
using GridAcademy.Modules.AiGeneration.Domain.Entities;
using GridAcademy.Modules.AiGeneration.Infrastructure.Llm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GridAcademy.Modules.AiGeneration.Services;

/// <summary>
/// Converts an approved QuestionDraft into a live Question row.
/// Looks up marks_master / negative_marks_master / complexity_levels by convention.
/// Sets IsAiGenerated = true on the resulting Question.
/// </summary>
public sealed class DraftConverter
{
    private readonly AppDbContext              _db;
    private readonly DuplicateDetector        _dupes;
    private readonly ILogger<DraftConverter>  _log;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DraftConverter(AppDbContext db, DuplicateDetector dupes, ILogger<DraftConverter> log)
    {
        _db    = db;
        _dupes = dupes;
        _log   = log;
    }

    /// <summary>
    /// Promotes a draft to a live Question. Sets draft.ApprovedQuestionId.
    /// Returns the new Question.Id.
    /// Throws if essential FK lookups fail.
    /// </summary>
    public async Task<Guid> PromoteAsync(QuestionDraft draft, Guid reviewerId, CancellationToken ct = default)
    {
        // ── Resolve FKs ──────────────────────────────────────────────────────
        var section = await _db.AiExamSections
            .FirstOrDefaultAsync(s => s.Id == draft.GenerationJob.AiExamSectionId, ct);

        int subjectId = draft.SubjectId
            ?? section?.SubjectId
            ?? throw new InvalidOperationException("Cannot promote draft: SubjectId unknown.");

        int topicId = draft.TopicId
            ?? 1; // fallback to first topic if not set (reviewer should set it)

        int difficultyId = draft.DifficultyLevelId
            ?? await _db.DifficultyLevels
                   .Where(d => d.Name == "Medium")
                   .Select(d => d.Id)
                   .FirstOrDefaultAsync(ct);

        // Auto-map complexity: same ordinal as difficulty (Easy→Low, Medium→Medium, Hard→High)
        int complexityId = await _db.ComplexityLevels
            .OrderBy(c => c.SortOrder)
            .Skip(await _db.DifficultyLevels.CountAsync(d => d.SortOrder < _db.DifficultyLevels
                .Where(x => x.Id == difficultyId).Select(x => x.SortOrder).FirstOrDefault(), ct))
            .Select(c => c.Id)
            .FirstOrDefaultAsync(ct);
        if (complexityId == 0)
            complexityId = (await _db.ComplexityLevels.FirstAsync(ct)).Id;

        // Use section marks if available, else default to "1 Mark"
        int marksId = section?.MarksId
            ?? await _db.MarksMaster.Where(m => m.Value == 1).Select(m => m.Id).FirstOrDefaultAsync(ct);
        int negMarksId = section?.NegativeMarksId
            ?? await _db.NegativeMarksMaster.Where(m => m.Value == 0).Select(m => m.Id).FirstOrDefaultAsync(ct);

        if (marksId == 0 || negMarksId == 0)
            throw new InvalidOperationException("Cannot promote draft: marks master lookup failed.");

        // ── Parse options from JSON ───────────────────────────────────────────
        var optionsArr = JsonNode.Parse(draft.OptionsJson)?.AsArray()
                         ?? throw new InvalidOperationException("draft.OptionsJson is invalid.");

        // ── Create Question ────────────────────────────────────────────────────
        var question = new Question
        {
            Text             = draft.QuestionText,
            Solution         = draft.Explanation,
            QuestionType     = QuestionType.MCQ,
            Status           = QuestionStatus.Published,
            SubjectId        = subjectId,
            TopicId          = topicId,
            DifficultyLevelId = difficultyId,
            ComplexityLevelId = complexityId,
            MarksId          = marksId,
            NegativeMarksId  = negMarksId,
            IsAiGenerated    = true,
            CreatedBy        = reviewerId,
            UpdatedBy        = reviewerId,
            CreatedAt        = DateTime.UtcNow,
            UpdatedAt        = DateTime.UtcNow
        };

        _db.Questions.Add(question);

        // ── Create Options ─────────────────────────────────────────────────────
        int sortOrder = 0;
        foreach (var opt in optionsArr)
        {
            var label     = opt?["label"]?.GetValue<string>() ?? "";
            var text      = opt?["text"]?.GetValue<string>()  ?? "";
            var isCorrect = opt?["is_correct"]?.GetValue<bool>() ?? false;

            _db.QuestionOptions.Add(new QuestionOption
            {
                Question  = question,
                Label     = label.Length > 0 ? label[0] : 'A',
                Text      = text,
                IsCorrect = isCorrect,
                SortOrder = sortOrder++
            });
        }

        // ── Update draft ───────────────────────────────────────────────────────
        draft.Status              = DraftStatus.Approved;
        draft.ReviewerId          = reviewerId;
        draft.ReviewedAt          = DateTime.UtcNow;
        draft.ApprovedQuestionId  = question.Id;

        await _db.SaveChangesAsync(ct);

        // ── Store embedding for future duplicate detection ─────────────────────
        _ = _dupes.UpsertEmbeddingAsync(question.Id, 1, question.Text, CancellationToken.None);

        _log.LogInformation("Draft {DraftId} promoted to Question {QuestionId}.", draft.Id, question.Id);
        return question.Id;
    }
}
