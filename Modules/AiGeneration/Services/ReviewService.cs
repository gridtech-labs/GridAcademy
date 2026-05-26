using GridAcademy.Data;
using GridAcademy.Modules.AiGeneration.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Modules.AiGeneration.Services;

/// <summary>Handles approve / reject / edit actions on QuestionDraft rows.</summary>
public sealed class ReviewService
{
    private readonly AppDbContext     _db;
    private readonly DraftConverter   _converter;

    public ReviewService(AppDbContext db, DraftConverter converter)
    {
        _db        = db;
        _converter = converter;
    }

    // ── Approve ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Approves a draft as-is and promotes it to a live Question.
    /// Returns the new Question.Id.
    /// </summary>
    public async Task<Guid> ApproveAsync(int draftId, Guid reviewerId, CancellationToken ct = default)
    {
        var draft = await LoadDraftAsync(draftId, ct);
        ValidateStatus(draft);

        return await _converter.PromoteAsync(draft, reviewerId, ct);
    }

    // ── Edit + Approve ────────────────────────────────────────────────────────

    /// <summary>
    /// Applies edits to a draft then promotes it. Reviewer corrected some fields.
    /// </summary>
    public async Task<Guid> EditAndApproveAsync(
        int     draftId,
        Guid    reviewerId,
        string? questionText,
        string? optionsJson,
        int?    correctIndex,
        string? explanation,
        int?    subjectId,
        int?    topicId,
        int?    difficultyLevelId,
        string? reviewNotes,
        CancellationToken ct = default)
    {
        var draft = await LoadDraftAsync(draftId, ct);
        ValidateStatus(draft);

        if (questionText    is not null) draft.QuestionText    = questionText;
        if (optionsJson     is not null) draft.OptionsJson     = optionsJson;
        if (correctIndex    is not null) draft.CorrectIndex    = correctIndex.Value;
        if (explanation     is not null) draft.Explanation     = explanation;
        if (subjectId       is not null) draft.SubjectId       = subjectId;
        if (topicId         is not null) draft.TopicId         = topicId;
        if (difficultyLevelId is not null) draft.DifficultyLevelId = difficultyLevelId;
        if (reviewNotes     is not null) draft.ReviewNotes     = reviewNotes;

        draft.Status = DraftStatus.EditedApproved;

        return await _converter.PromoteAsync(draft, reviewerId, ct);
    }

    // ── Reject ────────────────────────────────────────────────────────────────

    public async Task RejectAsync(int draftId, Guid reviewerId, string reason, CancellationToken ct = default)
    {
        var draft = await LoadDraftAsync(draftId, ct);
        ValidateStatus(draft);

        draft.Status      = DraftStatus.Rejected;
        draft.ReviewerId  = reviewerId;
        draft.ReviewNotes = reason;
        draft.ReviewedAt  = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    // ── Bulk ──────────────────────────────────────────────────────────────────

    /// <summary>Rejects all PendingReview drafts belonging to a job.</summary>
    public async Task BulkRejectJobAsync(int jobId, Guid reviewerId, CancellationToken ct = default)
    {
        var drafts = await _db.QuestionDrafts
            .Where(d => d.GenerationJobId == jobId
                     && d.Status          == DraftStatus.PendingReview)
            .ToListAsync(ct);

        foreach (var d in drafts)
        {
            d.Status     = DraftStatus.Rejected;
            d.ReviewerId = reviewerId;
            d.ReviewedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<QuestionDraft> LoadDraftAsync(int draftId, CancellationToken ct)
    {
        return await _db.QuestionDrafts
            .Include(d => d.GenerationJob)
                .ThenInclude(j => j.AiExamSection)
            .FirstOrDefaultAsync(d => d.Id == draftId, ct)
            ?? throw new KeyNotFoundException($"QuestionDraft {draftId} not found.");
    }

    private static void ValidateStatus(QuestionDraft draft)
    {
        if (draft.Status is DraftStatus.Approved or DraftStatus.EditedApproved)
            throw new InvalidOperationException($"Draft {draft.Id} is already approved.");
        if (draft.Status == DraftStatus.Rejected)
            throw new InvalidOperationException($"Draft {draft.Id} is already rejected.");
    }
}
