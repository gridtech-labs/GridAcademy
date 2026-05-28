using GridAcademy.Data;
using GridAcademy.Modules.AiGeneration.Domain.Entities;
using GridAcademy.Modules.AiGeneration.Infrastructure.Llm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GridAcademy.Modules.AiGeneration.Services;

/// <summary>
/// Persists daily LLM token usage to llm_usage table.
/// Also enforces a configurable daily quota safety margin.
/// </summary>
public sealed class LlmUsageTracker
{
    private readonly AppDbContext _db;
    private readonly ILogger<LlmUsageTracker> _log;

    public LlmUsageTracker(AppDbContext db, ILogger<LlmUsageTracker> log)
    {
        _db  = db;
        _log = log;
    }

    /// <summary>Appends a completed LLM call's tokens to today's running total.</summary>
    public async Task RecordAsync(LlmCompletion completion, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var row = await _db.LlmUsages
            .FirstOrDefaultAsync(r => r.Date == today
                                   && r.Provider == "gemini"
                                   && r.Model    == completion.Model, ct);

        if (row is null)
        {
            row = new LlmUsage
            {
                Date      = today,
                Provider  = "gemini",
                Model     = completion.Model,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.LlmUsages.Add(row);
        }

        row.PromptTokens      += completion.PromptTokens;
        row.CompletionTokens  += completion.CompletionTokens;
        row.TotalTokens       += completion.PromptTokens + completion.CompletionTokens;
        row.RequestCount      += 1;
        row.UpdatedAt          = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Non-fatal — never block generation over a tracking failure.
            // IMPORTANT: detach the entity from the change tracker so it doesn't
            // contaminate the next SaveChangesAsync call (e.g. for QuestionDrafts).
            // Without this, EF Core retries the failed insert in the next transaction
            // and rolls back the QuestionDraft saves too — questions are lost even
            // though Gemini already processed and billed the request.
            try { _db.Entry(row).State = Microsoft.EntityFrameworkCore.EntityState.Detached; }
            catch { /* ignore detach errors */ }

            _log.LogWarning(ex, "Failed to persist LLM usage stats (detached from tracker).");
        }
    }

    /// <summary>
    /// Returns today's total tokens across all models for a given provider.
    /// Used by GenerationService to enforce the daily quota safety margin.
    /// </summary>
    public async Task<int> GetTodayTokensAsync(string provider = "gemini", CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _db.LlmUsages
            .Where(r => r.Date == today && r.Provider == provider)
            .SumAsync(r => (int?)r.TotalTokens, ct) ?? 0;
    }
}
