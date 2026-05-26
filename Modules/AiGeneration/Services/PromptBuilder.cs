using GridAcademy.Data;
using GridAcademy.Modules.AiGeneration.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Modules.AiGeneration.Services;

/// <summary>
/// Builds the generation prompt text from:
///   • versioned GenerationPromptTemplate (if one exists for this config/section),
///   • or the built-in default template,
/// injected with the topic's syllabus and reference questions.
/// </summary>
public sealed class PromptBuilder
{
    private readonly AppDbContext _db;

    public PromptBuilder(AppDbContext db) => _db = db;

    public async Task<string> BuildAsync(
        GenerationJob job,
        AiExamTopic?  topic,
        CancellationToken ct = default)
    {
        // 1. Try to find the most recent active template for this section/exam
        var template = await _db.GenerationPromptTemplates
            .Where(t => t.AiExamConfigId == job.AiExamSection!.AiExamConfigId
                     && t.IsActive)
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync(ct);

        var basePrompt = template?.PromptText ?? DefaultTemplate();

        // 2. Inject context
        var syllabusBlock   = topic?.SyllabusText is { Length: > 0 } s
            ? $"SYLLABUS EXCERPT:\n{s}\n\n"
            : "";

        var referenceBlock  = topic?.ReferenceQuestionsJson is { Length: > 3 } r
            ? $"REFERENCE QUESTIONS (mimic style/difficulty, do NOT copy):\n{r}\n\n"
            : "";

        var sectionName     = job.AiExamSection?.SectionName ?? "General";
        var topicName       = topic?.TopicName ?? "General";
        var difficulty      = job.Difficulty;
        var count           = job.Count;
        var language        = job.Language == "hi" ? "Hindi" : "English";

        return basePrompt
            .Replace("{{SECTION}}", sectionName)
            .Replace("{{TOPIC}}", topicName)
            .Replace("{{DIFFICULTY}}", difficulty)
            .Replace("{{COUNT}}", count.ToString())
            .Replace("{{LANGUAGE}}", language)
            .Replace("{{SYLLABUS_BLOCK}}", syllabusBlock)
            .Replace("{{REFERENCE_BLOCK}}", referenceBlock);
    }

    // ── Default built-in template ─────────────────────────────────────────────
    private static string DefaultTemplate() =>
        """
        You are an expert question setter for competitive exams.

        Generate exactly {{COUNT}} high-quality multiple-choice questions for:
        - Section:    {{SECTION}}
        - Topic:      {{TOPIC}}
        - Difficulty: {{DIFFICULTY}}
        - Language:   {{LANGUAGE}}

        {{SYLLABUS_BLOCK}}{{REFERENCE_BLOCK}}

        RULES:
        1. Each question must have exactly 4 options (A, B, C, D).
        2. Exactly one option must be correct.
        3. All distractors must be plausible but clearly wrong to experts.
        4. No duplicate questions. No trivial questions.
        5. For numerical questions include step-by-step calculation in calculation_steps.
        6. Respond ONLY with a JSON array — no markdown fences, no extra text.

        JSON schema for each element:
        {
          "question_text": "...",
          "options": [
            {"label": "A", "text": "...", "is_correct": false},
            {"label": "B", "text": "...", "is_correct": true},
            {"label": "C", "text": "...", "is_correct": false},
            {"label": "D", "text": "...", "is_correct": false}
          ],
          "correct_index": 1,
          "explanation": "...",
          "calculation_steps": [{"op":"multiply","operands":[2,3],"result":6}],
          "difficulty_estimate": "medium",
          "estimated_seconds": 90
        }

        Now generate the {{COUNT}} questions.
        """;
}
