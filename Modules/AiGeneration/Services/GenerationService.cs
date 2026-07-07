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

    // Structured-output schema: forces the LLM to return an ARRAY of question objects,
    // so it cannot drift to unrelated JSON (e.g. exam/recruitment metadata) when the
    // section's syllabus/topic text is ambiguous. (Gemini schema — uppercase types.)
    private const string QuestionsResponseSchema = """
        {
          "type": "ARRAY",
          "items": {
            "type": "OBJECT",
            "properties": {
              "question_text": { "type": "STRING" },
              "options": {
                "type": "ARRAY",
                "items": {
                  "type": "OBJECT",
                  "properties": {
                    "label":      { "type": "STRING" },
                    "text":       { "type": "STRING" },
                    "is_correct": { "type": "BOOLEAN" }
                  },
                  "required": ["text", "is_correct"]
                }
              },
              "correct_index":       { "type": "INTEGER" },
              "explanation":         { "type": "STRING" },
              "difficulty_estimate": { "type": "STRING" },
              "estimated_seconds":   { "type": "INTEGER" }
            },
            "required": ["question_text", "options", "correct_index"]
          }
        }
        """;

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
                completion = await _llm.CompleteAsync(prompt, QuestionsResponseSchema, CancellationToken.None);
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
        // Serialize with the SAME snake_case keys the readers expect (ReviewService,
        // Review page ParseOptions, and DraftConverter all read "label"/"text"/
        // "is_correct"). Serializing the PascalCase record directly produced
        // {"Label","Text","IsCorrect"}, so option text was silently lost.
        var optionsJson       = JsonSerializer.Serialize(
            q.Options.Select(o => new { label = o.Label, text = o.Text, is_correct = o.IsCorrect }));
        var calcStepsJson     = q.CalculationSteps is { Count: > 0 }
            ? JsonSerializer.Serialize(
                q.CalculationSteps.Select(s => new { op = s.Op, operands = s.Operands, result = s.Result }))
            : null;
        var flagsJson         = flags.Count > 0
            ? JsonSerializer.Serialize(flags, _jsonOpts)
            : "{}";

        _db.QuestionDrafts.Add(new QuestionDraft
        {
            GenerationJobId      = job.Id,
            SubjectId            = job.SubjectId ?? job.AiExamSection?.SubjectId,
            TopicId              = job.TopicId ?? job.AiExamTopic?.TopicId,
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

        var root = JsonNode.Parse(raw.Trim())
                   ?? throw new InvalidOperationException("LLM returned empty or invalid JSON.");

        // Resolve to an array of question objects, tolerating the shapes Gemini
        // sometimes returns (esp. for math/arithmetic prompts):
        //   [ {...}, {...} ]                      ← ideal
        //   { "questions"|"data"|"items": [...] } ← wrapped in an object
        //   { "question_text": ... }              ← a single question object
        // Using `as JsonArray` (not .AsArray(), which THROWS on non-arrays).
        JsonArray? arr = root as JsonArray;
        if (arr is null && root is JsonObject obj)
        {
            arr = obj["questions"] as JsonArray
               ?? obj["data"]      as JsonArray
               ?? obj["items"]     as JsonArray;
            if (arr is null && obj.ContainsKey("question_text"))
                arr = new JsonArray(JsonNode.Parse(obj.ToJsonString())); // single question → wrap
        }
        if (arr is null)
            throw new InvalidOperationException(
                "LLM did not return a JSON array of questions. Response starts with: " +
                raw[..Math.Min(200, raw.Length)]);

        var result = new List<GeneratedQuestion>();
        foreach (var item in arr)
        {
            if (item is not JsonObject) continue;
            try
            {
                int correctIndex = int.TryParse(item["correct_index"]?.ToString(), out var ci) ? ci : 0;

                // Options can arrive as objects ({label,text,is_correct}) OR as plain
                // strings (common for math/arithmetic). Handle both, and if no per-option
                // is_correct is given, derive the correct one from correct_index.
                var opts = new List<GeneratedOption>();
                if (item["options"] is JsonArray optArr)
                {
                    for (int i = 0; i < optArr.Count; i++)
                    {
                        var o = optArr[i];
                        if (o is JsonObject oo)
                        {
                            var label = oo["label"]?.GetValue<string>();
                            opts.Add(new GeneratedOption(
                                string.IsNullOrEmpty(label) ? ((char)('A' + i)).ToString() : label,
                                oo["text"]?.GetValue<string>() ?? "",
                                (oo["is_correct"] as JsonValue)?.GetValue<bool>() ?? (i == correctIndex)));
                        }
                        else
                        {
                            // plain string / number option
                            opts.Add(new GeneratedOption(
                                ((char)('A' + i)).ToString(),
                                o?.ToString() ?? "",
                                i == correctIndex));
                        }
                    }
                    // Safety net: if nothing was marked correct, mark the correct_index one.
                    if (opts.Count > 0 && !opts.Any(x => x.IsCorrect) && correctIndex >= 0 && correctIndex < opts.Count)
                        opts[correctIndex] = opts[correctIndex] with { IsCorrect = true };
                }

                var steps = (item["calculation_steps"] as JsonArray)?
                    .Select(s => new CalculationStep(
                        s?["op"]?.GetValue<string>() ?? "",
                        (s?["operands"] as JsonArray)?
                            .Select(v => decimal.TryParse(v?.ToString(), out var d) ? d : 0m).ToList() ?? [],
                        decimal.TryParse(s?["result"]?.ToString(), out var r) ? r : 0m))
                    .ToList();

                int? estSeconds  = int.TryParse(item["estimated_seconds"]?.ToString(), out var es) ? es : null;

                result.Add(new GeneratedQuestion(
                    item["question_text"]?.GetValue<string>() ?? "",
                    opts,
                    correctIndex,
                    item["explanation"]?.GetValue<string>(),
                    steps,
                    item["difficulty_estimate"]?.GetValue<string>(),
                    estSeconds
                ));
            }
            catch
            {
                // Skip a single malformed question rather than failing the whole batch.
            }
        }

        if (result.Count == 0)
            throw new InvalidOperationException("LLM response parsed but contained no usable questions.");

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
