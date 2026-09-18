using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;
using GridAcademy.Data;
using GridAcademy.Data.Entities.Content;
using GridAcademy.DTOs.Content.Import;
using GridAcademy.Modules.AiGeneration.Infrastructure.Llm;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Services;

/// <summary>Where the questions go and how they are classified.</summary>
public record AiPdfImportOptions(
    int    SubjectId,
    int?   TopicId,
    Guid?  TestId,
    Guid?  ImportedBy,
    bool   PublishImmediately = false);

public interface IAiPdfImportService
{
    /// <summary>False when no Gemini API key is configured — the UI hides the tab.</summary>
    bool IsAvailable { get; }

    Task<ImportResultDto> ImportAsync(Stream pdf, string fileName, AiPdfImportOptions options, CancellationToken ct = default);
}

/// <summary>
/// Imports questions from a chapter-wise question-bank PDF by letting the LLM read the
/// document itself.
///
/// Why not the text parsers: publisher PDFs (MathonGo and similar) typeset maths as
/// separate text runs, so PdfPig returns formulas jumbled at the end of the page and the
/// question text arrives incomplete; diagrams are images and are lost entirely. The model
/// reads the page as a human does, including the answer key printed in a later section.
///
/// Questions are saved as <see cref="QuestionStatus.Draft"/> by default: the attempt engine
/// only serves Published questions, so nothing reaches students until a human has checked it.
/// </summary>
public class AiPdfImportService : IAiPdfImportService
{
    private readonly AppDbContext                  _db;
    private readonly ILLMProvider                  _llm;
    private readonly IConfiguration                _config;
    private readonly ILogger<AiPdfImportService>   _logger;

    public AiPdfImportService(AppDbContext db, ILLMProvider llm, IConfiguration config, ILogger<AiPdfImportService> logger)
    {
        _db      = db;
        _llm     = llm;
        _config  = config;
        _logger  = logger;
    }

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_config["Ai:Gemini:ApiKey"]);

    /// <summary>Added to questions that need a diagram the PDF holds only as an image.</summary>
    private const string FigureNote =
        "<p><em>[Figure required — add the diagram from the source PDF before publishing.]</em></p>";

    private const string Prompt = """
        You are extracting questions from an exam question-bank PDF so they can be loaded
        into a test platform. Read the whole document, including any answer key printed in
        a later section.

        Return EVERY question in the document, in the order they appear.

        For each question:
        - question_text: the complete question, as plain text. Write mathematics with Unicode
          symbols, NOT LaTeX: use ² ³ ₀ ₁ ₂ √ × ÷ π ε μ Ω ° ≈ ≤ ≥ ∫ Σ Δ and write fractions
          inline as (a)/(b) — for example "C = ε₀A/d", "K₁ = 2", "10 × 10⁻⁶ F".
          Do NOT include the question number or the exam/shift label in this field.
        - source_label: the exam and shift printed with the question, e.g.
          "JEE Main 2026 (21 January Shift 2)". Empty string if there is none.
        - is_numerical: true when the answer is a number the candidate types in (no options
          are offered); false for multiple choice.
        - options: for multiple choice, the options in order as plain text. Empty array when
          is_numerical is true. Do not include the "(1)" / "(A)" markers themselves.
        - correct_index: 0-based index of the correct option, taken from the document's answer
          key. Use -1 when is_numerical is true.
        - numerical_answer: the numeric answer from the answer key when is_numerical is true,
          as a plain number in a string (e.g. "8"). Empty string otherwise.
        - requires_figure: true when the question refers to a figure, diagram, circuit or graph
          that the document shows as a picture, or cannot be answered without seeing it.
        - difficulty: your own judgement — exactly "easy", "medium" or "hard".
        - solution: YOUR OWN concise worked solution, 2 to 6 lines, written from scratch in
          your own words. Do NOT copy, quote or paraphrase any solution text printed in the
          document — that text is someone else's copyrighted work. If you are not confident,
          return an empty string rather than guessing.

        Ignore page headers, footers, watermarks, branding, hashtags and website addresses.
        If a question is unreadable or its answer is not in the document, still return it with
        its best-effort text and requires_figure/difficulty filled in.

        The final page(s) of this extract may be an answer key listing answers by question
        number ("1. (3)  2. 8  3. (4) …"). Use it to fill correct_index / numerical_answer,
        matching on the question number. Do not return the answer key itself as a question.
        """;

    private const string ResponseSchema = """
        {
          "type": "ARRAY",
          "items": {
            "type": "OBJECT",
            "properties": {
              "question_text":    { "type": "STRING"  },
              "source_label":     { "type": "STRING"  },
              "is_numerical":     { "type": "BOOLEAN" },
              "options":          { "type": "ARRAY", "items": { "type": "STRING" } },
              "correct_index":    { "type": "INTEGER" },
              "numerical_answer": { "type": "STRING"  },
              "requires_figure":  { "type": "BOOLEAN" },
              "difficulty":       { "type": "STRING"  },
              "solution":         { "type": "STRING"  }
            },
            "required": ["question_text","is_numerical","options","correct_index",
                         "numerical_answer","requires_figure","difficulty","solution"]
          }
        }
        """;

    public async Task<ImportResultDto> ImportAsync(
        Stream pdf, string fileName, AiPdfImportOptions options, CancellationToken ct = default)
    {
        var result = new ImportResultDto { Source = "ai-pdf" };

        if (!IsAvailable)
        {
            result.Errors.Add(Err(0, "Configuration",
                "No Gemini API key is configured. Set Ai__Gemini__ApiKey before using AI PDF import."));
            return result;
        }

        // ── Masters ──────────────────────────────────────────────────────────
        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.Id == options.SubjectId, ct);
        if (subject is null)
        {
            result.Errors.Add(Err(0, "Subject", "Select a subject."));
            return result;
        }

        // Questions require a topic, and a chapter-wise import always has one.
        var topic = options.TopicId is null
            ? null
            : await _db.Topics.FirstOrDefaultAsync(t => t.Id == options.TopicId.Value, ct);
        if (topic is null)
        {
            result.Errors.Add(Err(0, "Topic", "Select the chapter these questions belong to."));
            return result;
        }
        if (topic.SubjectId != subject.Id)
        {
            result.Errors.Add(Err(0, "Topic",
                $"Chapter \"{topic.Name}\" does not belong to subject \"{subject.Name}\"."));
            return result;
        }

        var difficulties = await _db.DifficultyLevels.AsNoTracking().ToListAsync(ct);
        var complexities = await _db.ComplexityLevels.AsNoTracking().ToListAsync(ct);
        var marksList    = await _db.MarksMaster.AsNoTracking().ToListAsync(ct);
        var negMarksList = await _db.NegativeMarksMaster.AsNoTracking().ToListAsync(ct);

        if (difficulties.Count == 0 || complexities.Count == 0 || marksList.Count == 0 || negMarksList.Count == 0)
        {
            result.Errors.Add(Err(0, "Masters",
                "Difficulty, complexity, marks and negative-marks master data must exist before importing."));
            return result;
        }

        // JEE-style defaults, falling back to whatever the masters do contain
        var marks    = ByName(marksList,    m => m.Name, "4 Marks")  ?? marksList[0];
        var negMarks = ByName(negMarksList, n => n.Name, "-1 Mark")  ?? negMarksList[0];
        var mediumComplexity = ByName(complexities, c => c.Name, "Medium") ?? complexities[0];

        // ── Ask the model to read the PDF ────────────────────────────────────
        using var ms = new MemoryStream();
        await pdf.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        List<ExtractedQuestion> extracted;
        try
        {
            extracted = await ExtractAllAsync(bytes, fileName, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI PDF import: extraction failed for {File}", fileName);
            result.Errors.Add(Err(0, "AI", $"Could not read the PDF: {ex.Message}"));
            return result;
        }

        result.TotalRows = extracted.Count;
        if (extracted.Count == 0)
        {
            result.Errors.Add(Err(0, "AI", "No questions were found in this PDF."));
            return result;
        }

        // ── Persist ──────────────────────────────────────────────────────────
        // Grouped: the bank can already hold duplicate texts, which would throw here.
        var existingTexts = (await _db.Questions.AsNoTracking().Select(q => q.Text).ToListAsync(ct))
            .Select(t => t.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = new List<Question>();
        for (int i = 0; i < extracted.Count; i++)
        {
            var q      = extracted[i];
            var number = i + 1;
            var text   = (q.QuestionText ?? "").Trim();

            if (text.Length < 10)
            {
                result.Skipped++;
                result.Errors.Add(Err(number, "QuestionText", "Empty or too short — skipped."));
                continue;
            }

            if (!existingTexts.Add(text))
            {
                result.Duplicates++;
                continue;
            }

            var entity = new Question
            {
                Text              = q.RequiresFigure ? text + FigureNote : text,
                Solution          = string.IsNullOrWhiteSpace(q.Solution) ? null : q.Solution.Trim(),
                Subtopic          = string.IsNullOrWhiteSpace(q.SourceLabel) ? fileName : q.SourceLabel.Trim(),
                QuestionType      = q.IsNumerical ? QuestionType.NAT : QuestionType.MCQ,
                Status            = options.PublishImmediately ? QuestionStatus.Published : QuestionStatus.Draft,
                SubjectId         = subject.Id,
                TopicId           = topic.Id,
                DifficultyLevelId = MapDifficulty(difficulties, q.Difficulty).Id,
                ComplexityLevelId = mediumComplexity.Id,
                MarksId           = marks.Id,
                NegativeMarksId   = negMarks.Id,
                CreatedBy         = options.ImportedBy,
                UpdatedBy         = options.ImportedBy,
            };

            if (q.IsNumerical)
            {
                if (!decimal.TryParse(q.NumericalAnswer, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var num))
                {
                    result.Skipped++;
                    result.Errors.Add(Err(number, "NumericalAnswer",
                        $"No numeric answer found in the PDF for this question — skipped. ({Short(text)})"));
                    continue;
                }
                entity.NumericalAnswer = num;
            }
            else
            {
                var opts = (q.Options ?? []).Where(o => !string.IsNullOrWhiteSpace(o)).ToList();
                if (opts.Count < 2)
                {
                    result.Skipped++;
                    result.Errors.Add(Err(number, "Options",
                        $"Fewer than two options were read — skipped. ({Short(text)})"));
                    continue;
                }
                if (q.CorrectIndex < 0 || q.CorrectIndex >= opts.Count)
                {
                    result.Skipped++;
                    result.Errors.Add(Err(number, "CorrectOption",
                        $"The answer key gave no valid option for this question — skipped. ({Short(text)})"));
                    continue;
                }

                for (int o = 0; o < opts.Count; o++)
                    entity.Options.Add(new QuestionOption
                    {
                        Label     = (char)('A' + o),
                        Text      = opts[o].Trim(),
                        IsCorrect = o == q.CorrectIndex,
                        SortOrder = o,   // without this every option sorts equally and the order is arbitrary
                    });
            }

            if (q.RequiresFigure)
                result.Errors.Add(Err(number, "Figure",
                    $"Needs the diagram added before publishing. ({Short(text)})"));

            _db.Questions.Add(entity);
            added.Add(entity);
            result.Imported++;
        }

        if (added.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
            if (options.TestId.HasValue)
                await MapToTestAsync(added.Select(a => a.Id), options.TestId.Value, result, ct);
        }

        _logger.LogInformation(
            "AI PDF import: {File} → {Extracted} read, {Imported} imported, {Skipped} skipped, {Dupes} duplicates.",
            fileName, extracted.Count, result.Imported, result.Skipped, result.Duplicates);

        return result;
    }

    // ── Extraction ───────────────────────────────────────────────────────────

    /// <summary>
    /// Reads the document in small page-chunks rather than one request. A whole chapter PDF
    /// sent at once is a multi-megabyte upload and a long single generation — connections get
    /// cut mid-transfer (seen with a 1.3 MB file) and large banks would not fit in one reply.
    /// The answer key page travels with every chunk so answers still resolve.
    /// </summary>
    private async Task<List<ExtractedQuestion>> ExtractAllAsync(byte[] bytes, string fileName, CancellationToken ct)
    {
        var pagesPerChunk = int.TryParse(_config["Ai:PdfImport:PagesPerChunk"], out var p) && p > 0 ? p : 2;
        var chunks        = BuildChunks(bytes, pagesPerChunk);

        var all  = new List<ExtractedQuestion>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (chunkBytes, label) in chunks)
        {
            ct.ThrowIfCancellationRequested();
            var completion = await _llm.CompleteWithFileAsync(Prompt, chunkBytes, "application/pdf", ResponseSchema, ct);

            List<ExtractedQuestion> part;
            try
            {
                part = Parse(completion.Text);
            }
            catch (Exception ex)
            {
                // One unreadable chunk must not lose the rest of the chapter.
                _logger.LogWarning(ex, "AI PDF import: {File} {Label} — unreadable response, skipped.", fileName, label);
                continue;
            }

            int kept = 0;
            foreach (var q in part)
            {
                if (string.IsNullOrWhiteSpace(q.QuestionText)) continue;
                if (!seen.Add(q.QuestionText.Trim())) continue;   // chunks can overlap
                all.Add(q);
                kept++;
            }

            _logger.LogInformation("AI PDF import: {File} {Label} ({Kb} KB) → {Kept} new question(s).",
                fileName, label, chunkBytes.Length / 1024, kept);
        }

        return all;
    }

    /// <summary>
    /// Splits the PDF into chunks of question pages, appending the answer-key page(s) to each.
    /// Returns the file unchanged when it is already small enough.
    /// </summary>
    private static List<(byte[] Bytes, string Label)> BuildChunks(byte[] pdfBytes, int pagesPerChunk)
    {
        using var doc = PdfDocument.Open(pdfBytes);
        var pageCount = doc.NumberOfPages;

        if (pageCount <= pagesPerChunk)
            return [(pdfBytes, $"pages 1-{pageCount}")];

        // Where do the answers start? Everything from there on is the solutions section.
        // Start at page 2: page 1 is always questions, and running banners such as
        // "Questions with Answer Keys" appear on every page — strip those before matching.
        int firstAnswerPage = 0;
        for (int page = 2; page <= pageCount; page++)
        {
            var text = doc.GetPage(page).Text ?? "";
            text = Regex.Replace(text, @"questions?\s+with\s+answers?\s+keys?", " ", RegexOptions.IgnoreCase);

            if (Regex.IsMatch(text, @"answers?\s*(and|&)\s*solutions?|answer\s*key\b", RegexOptions.IgnoreCase))
            {
                firstAnswerPage = page;
                break;
            }
        }

        // Only the first page or two of that section holds the key list itself; sending the
        // whole solutions section with every chunk would bloat each request again.
        var keyPages = firstAnswerPage == 0
            ? []
            : Enumerable.Range(firstAnswerPage, Math.Min(2, pageCount - firstAnswerPage + 1)).ToList();
        var lastQuestionPage = firstAnswerPage == 0 ? pageCount : firstAnswerPage - 1;

        var chunks = new List<(byte[], string)>();
        for (int start = 1; start <= lastQuestionPage; start += pagesPerChunk)
        {
            var end     = Math.Min(start + pagesPerChunk - 1, lastQuestionPage);
            var builder = new PdfDocumentBuilder();
            for (int page = start; page <= end; page++) builder.AddPage(doc, page);
            foreach (var page in keyPages) builder.AddPage(doc, page);

            chunks.Add((builder.Build(), $"pages {start}-{end}"));
        }

        // Safety net: never return nothing to read (e.g. if the answer-section detection
        // swallowed every page) — fall back to sending the document as it is.
        return chunks.Count > 0 ? chunks : [(pdfBytes, $"pages 1-{pageCount}")];
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ImportRowError Err(int row, string field, string message)
        => new() { Row = row, Field = field, Message = message };

    private static string Short(string text)
        => text.Length <= 60 ? text : text[..60] + "…";

    private static T? ByName<T>(List<T> list, Func<T, string> name, string wanted) where T : class
        => list.FirstOrDefault(x => name(x).Equals(wanted, StringComparison.OrdinalIgnoreCase));

    private static DifficultyLevel MapDifficulty(List<DifficultyLevel> levels, string? difficulty)
    {
        var wanted = (difficulty ?? "").Trim().ToLowerInvariant() switch
        {
            "easy" => "Easy",
            "hard" => "Hard",
            _      => "Medium",
        };
        return ByName(levels, l => l.Name, wanted)
            ?? ByName(levels, l => l.Name, "Medium")
            ?? levels[0];
    }

    private async Task MapToTestAsync(IEnumerable<Guid> questionIds, Guid testId, ImportResultDto result, CancellationToken ct)
    {
        var test = await _db.Tests.AsNoTracking().FirstOrDefaultAsync(t => t.Id == testId, ct);
        if (test is null) return;

        var existing = (await _db.TestQuestions.Where(tq => tq.TestId == testId)
                .Select(tq => tq.QuestionId).ToListAsync(ct))
            .ToHashSet();

        int order = existing.Count + 1;
        foreach (var id in questionIds)
        {
            if (!existing.Add(id)) continue;
            _db.TestQuestions.Add(new Data.Entities.Assessment.TestQuestion
            {
                TestId = testId, QuestionId = id, SortOrder = order++,
            });
        }

        await _db.SaveChangesAsync(ct);
        result.MappedTestName = test.Title;
    }

    private sealed class ExtractedQuestion
    {
        public string?        QuestionText    { get; init; }
        public string?        SourceLabel     { get; init; }
        public bool           IsNumerical     { get; init; }
        public List<string>?  Options         { get; init; }
        public int            CorrectIndex    { get; init; } = -1;
        public string?        NumericalAnswer { get; init; }
        public bool           RequiresFigure  { get; init; }
        public string?        Difficulty      { get; init; }
        public string?        Solution        { get; init; }
    }

    /// <summary>
    /// Tolerant parse: the schema asks for a bare array, but models occasionally wrap it in
    /// an object or fence it in markdown.
    /// </summary>
    private static List<ExtractedQuestion> Parse(string raw)
    {
        raw = (raw ?? "").Trim();
        if (raw.StartsWith("```", StringComparison.Ordinal))
        {
            var nl = raw.IndexOf('\n');
            if (nl > 0) raw = raw[(nl + 1)..];
            if (raw.EndsWith("```", StringComparison.Ordinal)) raw = raw[..^3];
        }

        var root = JsonNode.Parse(raw.Trim()) ?? throw new InvalidOperationException("empty response");
        var arr  = root as JsonArray
                   ?? (root as JsonObject)?["questions"] as JsonArray
                   ?? (root as JsonObject)?["items"]     as JsonArray
                   ?? throw new InvalidOperationException("the response was not a list of questions");

        var list = new List<ExtractedQuestion>();
        foreach (var item in arr)
        {
            if (item is not JsonObject o) continue;
            try
            {
                list.Add(new ExtractedQuestion
                {
                    QuestionText    = o["question_text"]?.GetValue<string>(),
                    SourceLabel     = o["source_label"]?.GetValue<string>(),
                    IsNumerical     = o["is_numerical"]?.GetValue<bool>() ?? false,
                    Options         = (o["options"] as JsonArray)?.Select(x => x?.ToString() ?? "").ToList(),
                    CorrectIndex    = int.TryParse(o["correct_index"]?.ToString(), out var ci) ? ci : -1,
                    NumericalAnswer = o["numerical_answer"]?.ToString(),
                    RequiresFigure  = o["requires_figure"]?.GetValue<bool>() ?? false,
                    Difficulty      = o["difficulty"]?.GetValue<string>(),
                    Solution        = o["solution"]?.GetValue<string>(),
                });
            }
            catch
            {
                // skip a single malformed item rather than losing the whole document
            }
        }
        return list;
    }
}
