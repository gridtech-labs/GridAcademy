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
        // 1. Try to find the most recent active template for this section/exam.
        //    AiExamSection is optional — jobs can be created without a specific section.
        var configId = job.AiExamSection?.AiExamConfigId;
        var template = configId.HasValue
            ? await _db.GenerationPromptTemplates
                .Where(t => t.AiExamConfigId == configId.Value && t.IsActive)
                .OrderByDescending(t => t.Version)
                .FirstOrDefaultAsync(ct)
            : null;

        var basePrompt = template?.PromptText ?? DefaultTemplate();

        // 2. Inject context
        var syllabusBlock   = topic?.SyllabusText is { Length: > 0 } s
            ? $"SYLLABUS EXCERPT:\n{s}\n\n"
            : "";

        var referenceBlock  = topic?.ReferenceQuestionsJson is { Length: > 3 } r
            ? $"REFERENCE QUESTIONS (mimic style/difficulty, do NOT copy):\n{r}\n\n"
            : "";

        // Master Subject / Topic chosen on the job take priority; fall back to the
        // AI-section name / AI-topic name for older jobs.
        var subjectName = job.SubjectId.HasValue
            ? await _db.Subjects.Where(x => x.Id == job.SubjectId.Value).Select(x => x.Name).FirstOrDefaultAsync(ct)
              ?? job.AiExamSection?.SectionName ?? "General"
            : job.AiExamSection?.SectionName ?? "General";

        var topicName = job.TopicId.HasValue
            ? await _db.Topics.Where(x => x.Id == job.TopicId.Value).Select(x => x.Name).FirstOrDefaultAsync(ct)
              ?? topic?.TopicName ?? "General"
            : topic?.TopicName ?? "General";

        // The Exam conveys the STANDARD/LEVEL (e.g. "IIT JEE Advanced" vs "NEET UG"),
        // so the model calibrates difficulty and style to that exam.
        var examName = await _db.ExamPages
            .Where(e => e.Id == job.ExamPageId).Select(e => e.Title).FirstOrDefaultAsync(ct)
            ?? "a competitive exam";

        var difficulty      = job.Difficulty;
        var count           = job.Count;
        var language        = job.Language == "hi" ? "Hindi" : "English";

        return basePrompt
            .Replace("{{EXAM}}", examName)
            .Replace("{{SUBJECT}}", subjectName)
            .Replace("{{SECTION}}", subjectName)   // legacy templates use {{SECTION}}
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

        Generate exactly {{COUNT}} high-quality multiple-choice questions at the
        standard, style and difficulty of this exam: {{EXAM}}.
        - Subject:    {{SUBJECT}}
        - Topic:      {{TOPIC}}
        - Difficulty: {{DIFFICULTY}}
        - Language:   {{LANGUAGE}}

        Match the depth and question pattern typical of {{EXAM}} for {{SUBJECT}}.

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
