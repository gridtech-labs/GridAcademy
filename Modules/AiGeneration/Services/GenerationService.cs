using GridAcademy.Data;
using GridAcademy.Modules.AiGeneration.Domain.Entities;
using GridAcademy.Modules.AiGeneration.Infrastructure.Llm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GridAcademy.Modules.AiGeneration.Services;

/// <summary>
/// Core generation pipeline. Called by GenerationWorkerJob inside a Hangfire background job.
///
/// Pipeline per batch:
///   1. Build prompt
///   2. Call LLM → parse JSON array of GeneratedQuestion
///   3. SelfVerify each question (optional)
///   4. MathRecompute each question (optional)
///   5. DuplicateDetect each question (optional)
///   6. Persist as QuestionDraft rows with flag JSON
///   7. Track LLM usage
/// </summary>
public sealed class GenerationService
{
    private readonly AppDbContext       _db;
    private readonly ILLMProvider       _llm;
    private readonly PromptBuilder      _promptBuilder;
    private readonly SelfVerifier       _selfVerifier;
    private readonly MathRecomputer     _mathRecomputer;
    private readonly DuplicateDetector  _duplicateDetector;
    private readonly LlmUsageTracker    _usageTracker;
    private readonly bool               _enableSelfVerification;
    private readonly bool               _enableMathRecompute;
    private readonly bool               _enableDuplicateCheck;
    private readonly int                _maxPerBatch;
    private readonly ILogger<GenerationService> _log;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GenerationService(
        AppDbContext        db,
        ILLMProvider        llm,
        PromptBuilder       promptBuilder,
        SelfVerifier        selfVerifier,
        MathRecomputer      mathRecomputer,
        DuplicateDetector   duplicateDetector,
        LlmUsageTracker     usageTracker,
        IConfiguration      cfg,
        ILogger<GenerationService> log)
    {
        _db                    = db;
        _llm                   = llm;
        _promptBuilder         = promptBuilder;
        _selfVerifier          = selfVerifier;
        _mathRecomputer        = mathRecomputer;
        _duplicateDetector     = duplicateDetector;
        _usageTracker          = usageTracker;
        _log                   = log;
        _enableSelfVerification = cfg.GetValue<bool>("Ai:Generation:EnableSelfVerification", true);
        _enableMathRecompute    = cfg.GetValue<bool>("Ai:Generation:EnableMathRecompute", true);
        _enableDuplicateCheck   = cfg.GetValue<bool>("Ai:Generation:EnableDuplicateCheck", true);
        // Keep batches small so the JSON array fits within the model's output-token
        // limit (gemini-2.0-flash = 8192). Larger batches truncate mid-JSON → parse fail.
        // Larger Count values still work — the pipeline loops over multiple batches.
        _maxPerBatch            = cfg.GetValue<int> ("Ai:Generation:MaxQuestionsPerBatch", 5);
    }

    /// <summary>
    /// Runs one generation job end-to-end. Updates GenerationJob.Status in-place.
    /// </summary>
    public async Task RunJobAsync(int jobId, CancellationToken ct = default)
    {
        var job = await _db.GenerationJobs
            .Include(j => j.AiExamSection)
                .ThenInclude(s => s!.AiExamConfig)
            .Include(j => j.AiExamTopic)
            .FirstOrDefaultAsync(j => j.Id == jobId, ct)
            ?? throw new InvalidOperationException($"GenerationJob {jobId} not found.");

        job.Status = GenerationJobStatus.Running;
        await _db.SaveChangesAsync(ct);

        try
        {
            await ExecuteAsync(job, ct);
            job.Status      = GenerationJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "GenerationJob {JobId} failed.", jobId);
            job.Status       = GenerationJobStatus.Failed;
            job.ErrorMessage = ex.Message[..Math.Min(1000, ex.Message.Length)];
            job.CompletedAt  = DateTime.UtcNow;
        }
        finally
        {
            await _db.SaveChangesAsync(CancellationToken.None);
        }
    }

    // ── Internal pipeline ─────────────────────────────────────────────────────

    private async Task ExecuteAsync(GenerationJob job, CancellationToken ct)
    {
        int remaining = Math.Min(job.Count, 200); // safety cap
        int batchSize = Math.Min(remaining, _maxPerBatch);

        while (remaining > 0)
        {
            // Check cancellation only between batches — never mid-call.
            // Once a Gemini call starts, Google has already received it and will
            // charge for it regardless of whether we cancel on our side.
            // Cancelling mid-call means we pay but save nothing.
            ct.ThrowIfCancellationRequested();

            int thisBatch = Math.Min(remaining, batchSize);
            var jobCopy   = CloneJobWithCount(job, thisBatch);
            var topic     = job.AiExamTopic;

            // 1. Build prompt
            var prompt = await _promptBuilder.BuildAsync(jobCopy, topic, CancellationToken.None);

            // 2. LLM call — use CancellationToken.None so we always read the response.
            //    Cancelling mid-HTTP means Gemini processed the request (billed) but
            //    we discard the result and save nothing. Always complete the call.
            LlmCompletion completion;
            try
            {
                completion = await _llm.CompleteAsync(prompt, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "LLM call failed for job {JobId}.", job.Id);
                throw;
            }

            await _usageTracker.RecordAsync(completion, CancellationToken.None);
            job.PromptTemplateVersion = await GetActiveTemplateVersionAsync(job, CancellationToken.None);

            // 3. Parse JSON array
            List<GeneratedQuestion> questions;
            try
            {
                questions = ParseQuestions(completion.Text);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "JSON parse failed for job {JobId}. Raw: {Raw}",
                    job.Id, completion.Text[..Math.Min(500, completion.Text.Length)]);
                throw;
            }

            // 4. Process and persist each question immediately.
            //    Save after each question (not the whole batch) so a restart
            //    only loses the current in-flight question, not the entire batch.
            foreach (var q in questions)
            {
                await ProcessOneAsync(job, q, CancellationToken.None);
                await _db.SaveChangesAsync(CancellationToken.None); // never cancel a save
            }

            remaining -= thisBatch;
        }
    }

    private async Task ProcessOneAsync(GenerationJob job, GeneratedQuestion q, CancellationToken ct)
    {
        var flags = new Dictionary<string, object?>();

        // ── Self-verification ─────────────────────────────────────────────────
        if (_enableSelfVerification)
        {
            var ver = await _selfVerifier.VerifyAsync(q, ct);
            if (!ver.Matches)
            {
                flags["self_verification_mismatch"] = true;
                flags["verifier_answer"] = ver.VerifierAnswer;
                job.AutoFlagged++;
            }
        }

        // ── Math recompute ────────────────────────────────────────────────────
        if (_enableMathRecompute && !_mathRecomputer.Validate(q))
        {
            flags["math_mismatch"] = true;
            job.AutoFlagged++;
        }

        // ── Duplicate detection ────────────────────────────────────────────────
        if (_enableDuplicateCheck)
        {
            var dup = await _duplicateDetector.FindDuplicateAsync(q.QuestionText, ct);
            if (dup.HasValue)
            {
                flags["possible_duplicate"] = new { matched_id = dup.Value.MatchedId, score = dup.Value.Score };
                job.AutoFlagged++;
            }
        }

        // ── Persist draft ─────────────────────────────────────────────────────
        var optionsJson       = JsonSerializer.Serialize(q.Options,        _jsonOpts);
        var calcStepsJson     = q.CalculationSteps is { Count: > 0 }
            ? JsonSerializer.Serialize(q.CalculationSteps, _jsonOpts)
            : null;
        var flagsJson         = flags.Count > 0
            ? JsonSerializer.Serialize(flags, _jsonOpts)
            : "{}";

        _db.QuestionDrafts.Add(new QuestionDraft
        {
            GenerationJobId      = job.Id,
            SubjectId            = job.AiExamSection?.SubjectId,
            TopicId              = job.AiExamTopic?.TopicId,
            DifficultyLevelId    = null, // reviewer sets this
            Language             = job.Language,
            QuestionText         = q.QuestionText,
            OptionsJson          = optionsJson,
            CorrectIndex         = q.CorrectIndex,
            Explanation          = q.Explanation,
            CalculationStepsJson = calcStepsJson,
            DifficultyEstimate   = q.DifficultyEstimate,
            EstimatedSeconds     = q.EstimatedSeconds,
            FlagsJson            = flagsJson,
            Status               = flags.Count > 0 ? DraftStatus.PendingReview : DraftStatus.PendingReview,
            Model                = _llm.ModelName,
            PromptTemplateVersion = job.PromptTemplateVersion,
            CreatedAt            = DateTime.UtcNow
        });

        job.Generated++;

        // Cache embedding for future duplicate checks (fire and forget)
        if (_enableDuplicateCheck)
            _ = _duplicateDetector.UpsertEmbeddingAsync(Guid.NewGuid(), 2, q.QuestionText, CancellationToken.None);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<GeneratedQuestion> ParseQuestions(string text)
    {
        // Strip potential markdown fences
        var raw = text.Trim();
        if (raw.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = raw.IndexOf('\n');
            if (firstNewline > 0) raw = raw[(firstNewline + 1)..];
            if (raw.EndsWith("```", StringComparison.Ordinal))
                raw = raw[..^3];
        }

        var arr = JsonNode.Parse(raw.Trim())?.AsArray()
                  ?? throw new InvalidOperationException("LLM did not return a JSON array.");

        var result = new List<GeneratedQuestion>();
        foreach (var item in arr)
        {
            if (item is null) continue;

            var opts = item["options"]?.AsArray()?
                .Select(o => new GeneratedOption(
                    o?["label"]?.GetValue<string>() ?? "",
                    o?["text"]?.GetValue<string>()  ?? "",
                    o?["is_correct"]?.GetValue<bool>() ?? false))
                .ToList() ?? [];

            var steps = item["calculation_steps"]?.AsArray()?
                .Select(s => new CalculationStep(
                    s?["op"]?.GetValue<string>() ?? "",
                    s?["operands"]?.AsArray()?.Select(v => v!.GetValue<decimal>()).ToList() ?? [],
                    s?["result"]?.GetValue<decimal>() ?? 0))
                .ToList();

            result.Add(new GeneratedQuestion(
                item["question_text"]?.GetValue<string>() ?? "",
                opts,
                item["correct_index"]?.GetValue<int>() ?? 0,
                item["explanation"]?.GetValue<string>(),
                steps,
                item["difficulty_estimate"]?.GetValue<string>(),
                item["estimated_seconds"]?.GetValue<int>()
            ));
        }

        return result;
    }

    private static GenerationJob CloneJobWithCount(GenerationJob job, int count)
    {
        // Shallow clone for prompt building — only count changes
        return new GenerationJob
        {
            Id               = job.Id,
            ExamPageId       = job.ExamPageId,
            AiExamSectionId  = job.AiExamSectionId,
            AiExamTopicId    = job.AiExamTopicId,
            Difficulty       = job.Difficulty,
            Language         = job.Language,
            Count            = count,
            Notes            = job.Notes,
            AiExamSection    = job.AiExamSection,
            AiExamTopic      = job.AiExamTopic
        };
    }

    private async Task<int?> GetActiveTemplateVersionAsync(GenerationJob job, CancellationToken ct)
    {
        if (job.AiExamSection is null) return null;
        return await _db.GenerationPromptTemplates
            .Where(t => t.AiExamConfigId == job.AiExamSection.AiExamConfigId && t.IsActive)
            .OrderByDescending(t => t.Version)
            .Select(t => (int?)t.Version)
            .FirstOrDefaultAsync(ct);
    }
}
