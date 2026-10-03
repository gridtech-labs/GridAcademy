using System.Text.Json.Nodes;
using GridAcademy.Data;
using GridAcademy.Data.Entities.Content;
using GridAcademy.DTOs.Content.Import;
using GridAcademy.Modules.AiGeneration.Infrastructure.Llm;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;

namespace GridAcademy.Services.PyqBook;

/// <summary>What to import from a PYQ book, and where it should land.</summary>
public record PyqImportOptions(
    int   SubjectId,
    int   FromPage,
    int   ToPage,
    Guid? TestId              = null,
    Guid? ImportedBy          = null,
    int   PagesPerChunk       = 2,
    int   MaxChunks           = 0,      // 0 = no limit; a small value keeps a trial cheap
    bool  PublishImmediately  = false);

public interface IPyqBookImportService
{
    bool IsAvailable { get; }
    Task<ImportResultDto> ImportAsync(Stream pdf, string fileName, PyqImportOptions options, CancellationToken ct = default);
}

/// <summary>
/// Imports a chapter-wise PYQ book (MathonGo and similar) into the question bank.
///
/// Division of labour — deliberately, the model is only asked for what a machine cannot read:
///
///   answers    → parsed from the book's answer key in code (exact; see PyqBookStructure)
///   chapter    → reported by the model, which sees the "CHAPTER n" divider drawn as graphics,
///                and carried forward across pages that show no header
///   difficulty → the book's own Concept Builder / Must Do / Advanced grouping
///   text, options, sub-topic → read by the model from the page image
///
/// Everything lands as Draft, so nothing reaches students before a human has checked it.
/// Questions whose options are pictures (common in Organic Chemistry) are counted and skipped
/// rather than saved with empty options — they need the image pipeline.
/// </summary>
public class PyqBookImportService : IPyqBookImportService
{
    private readonly AppDbContext                    _db;
    private readonly ILLMProvider                    _llm;
    private readonly IConfiguration                  _config;
    private readonly ILogger<PyqBookImportService>   _logger;

    public PyqBookImportService(AppDbContext db, ILLMProvider llm, IConfiguration config,
                                ILogger<PyqBookImportService> logger)
    {
        _db = db; _llm = llm; _config = config; _logger = logger;
    }

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_config["Ai:Gemini:ApiKey"]);

    private const string FigureNotePrefix = "<p><em>[Figure required — ";

    private const string Prompt = """
        These pages come from a previous-years' question book for a competitive exam. Read them
        and return every question that appears, in order.

        For each question:
        - question_number: the number printed with it (the "Q104." marker) as an integer. This is
          essential — it is how the answer is matched. Use 0 only if no number is visible.
        - chapter_title: the chapter name printed on the page — as a running header, or on a
          "CHAPTER n" divider page. Return "" when the page shows none; do not guess it from the
          subject matter.
        - subtopic: the nearest sub-heading above the question, e.g. "Conductance and
          Conductivity". "" if there is none.
        - category: "Concept Builder", "Must Do" or "Advanced" if the question sits under one of
          those headings, else "".
        - question_text: the complete question as plain text. Write mathematics with Unicode
          symbols, NOT LaTeX: ² ³ ₀ ₁ ₂ √ × ÷ π ε μ Ω ° ≈ ≤ ≥ ∫ Σ Δ → ⇌, and fractions inline as
          (a)/(b). Do not include the question number or the marks.
        - is_numerical: true when the answer is a number the candidate types in (no options are
          offered); false for multiple choice.
        - options: the options in order as plain text. Empty array when is_numerical is true, or
          when the options are pictures (see below). Do not include the "(1)" markers.
        - options_are_images: true when the options are pictures — chemical structures, reaction
          schemes, graphs — rather than text. Set this instead of describing them.
        - requires_figure: true when the question itself refers to a figure, diagram, graph or
          structure shown as a picture.
        - figure_description: when requires_figure is true, describe it precisely enough to redraw
          without this book. "" otherwise.

        Do NOT return the correct answer — the answer key is read separately.
        Ignore page headers, footers, watermarks, branding and page numbers.
        A page that carries no questions (a divider or an analysis page) returns nothing for it.
        """;

    private const string ResponseSchema = """
        {
          "type": "ARRAY",
          "items": {
            "type": "OBJECT",
            "properties": {
              "question_number":    { "type": "INTEGER" },
              "chapter_title":      { "type": "STRING"  },
              "subtopic":           { "type": "STRING"  },
              "category":           { "type": "STRING"  },
              "question_text":      { "type": "STRING"  },
              "is_numerical":       { "type": "BOOLEAN" },
              "options":            { "type": "ARRAY", "items": { "type": "STRING" } },
              "options_are_images": { "type": "BOOLEAN" },
              "requires_figure":    { "type": "BOOLEAN" },
              "figure_description": { "type": "STRING"  }
            },
            "required": ["question_number","chapter_title","subtopic","category","question_text",
                         "is_numerical","options","options_are_images","requires_figure",
                         "figure_description"]
          }
        }
        """;

    public async Task<ImportResultDto> ImportAsync(
        Stream pdf, string fileName, PyqImportOptions options, CancellationToken ct = default)
    {
        var result = new ImportResultDto { Source = "pyq-book" };

        if (!IsAvailable)
        {
            result.Errors.Add(Err(0, "Configuration", "No Gemini API key is configured."));
            return result;
        }

        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.Id == options.SubjectId, ct);
        if (subject is null)
        {
            result.Errors.Add(Err(0, "Subject", "Select a subject."));
            return result;
        }

        var masters = await LoadMastersAsync(ct);
        if (masters is null)
        {
            result.Errors.Add(Err(0, "Masters", "Difficulty, complexity, marks and negative-marks masters must exist."));
            return result;
        }

        using var ms = new MemoryStream();
        await pdf.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        // The book's own answer key — exact, and the reason no answer is ever guessed.
        var book = PyqBookStructure.Read(bytes);
        if (book.Chapters.Count == 0)
        {
            result.Errors.Add(Err(0, "AnswerKey",
                "No answer key could be read from this PDF, so answers cannot be matched. " +
                "Include the answer-key pages in the file."));
            return result;
        }

        var from = Math.Max(1, options.FromPage);
        var to   = Math.Min(options.ToPage > 0 ? options.ToPage : book.AnswerKeyStartPage - 1,
                            book.AnswerKeyStartPage - 1);
        if (to < from)
        {
            result.Errors.Add(Err(0, "Pages", $"No question pages in range {from}-{to}."));
            return result;
        }

        var chunks = PyqBookStructure.Chunks(from, to, Math.Max(1, options.PagesPerChunk)).ToList();
        if (options.MaxChunks > 0) chunks = chunks.Take(options.MaxChunks).ToList();

        var existingTexts = (await _db.Questions.AsNoTracking().Select(q => q.Text).ToListAsync(ct))
            .Select(t => t.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var topicCache   = new Dictionary<string, Topic>(StringComparer.OrdinalIgnoreCase);
        var added        = new List<Question>();
        var imageOptions = 0;
        var unmatched    = 0;
        string? currentChapter = null;          // carried across pages that show no header

        foreach (var (chunkFrom, chunkTo) in chunks)
        {
            ct.ThrowIfCancellationRequested();

            // Retry transient failures. Over a whole book this loop runs ~240 times, so a passing
            // network blip is certain — one was seen losing two pages (~24 questions) outright.
            // The provider already retries rate limits; this covers DNS and dropped connections.
            List<Extracted>? extracted = null;
            Exception? lastError = null;

            for (var attempt = 1; attempt <= 3 && extracted is null; attempt++)
            {
                try
                {
                    var slice = Slice(bytes, chunkFrom, chunkTo);
                    var completion = await _llm.CompleteWithFileAsync(Prompt, slice, "application/pdf", ResponseSchema, ct);
                    extracted = Parse(completion.Text);
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    _logger.LogWarning("PYQ import: pages {From}-{To} attempt {Attempt}/3 failed: {Message}",
                        chunkFrom, chunkTo, attempt, ex.Message);
                    if (attempt < 3) await Task.Delay(TimeSpan.FromSeconds(5 * attempt), ct);
                }
            }

            if (extracted is null)
            {
                result.Errors.Add(Err(chunkFrom, "Pages",
                    $"Pages {chunkFrom}-{chunkTo} could not be read after 3 attempts: {lastError?.Message}"));
                continue;
            }

            result.TotalRows += extracted.Count;

            foreach (var q in extracted)
            {
                // Only accept a reported name that is actually one of the key's chapters. Pages
                // often print the unit ("Physical Chemistry") in the header instead of the
                // chapter; taking that at face value loses every question until the next
                // recognisable header.
                if (!string.IsNullOrWhiteSpace(q.ChapterTitle))
                {
                    var reported = MatchChapter(book.Chapters, q.ChapterTitle);
                    if (reported is not null) currentChapter = reported.Title;
                }

                var text = (q.QuestionText ?? "").Trim();
                if (text.Length < 10) { result.Skipped++; continue; }

                if (q.OptionsAreImages)
                {
                    imageOptions++;
                    result.Skipped++;
                    continue;                    // needs the image pipeline, not a half-saved question
                }

                var chapter = MatchChapter(book.Chapters, currentChapter);
                if (chapter is null || q.QuestionNumber <= 0 ||
                    !chapter.Answers.TryGetValue(q.QuestionNumber, out var answer))
                {
                    unmatched++;
                    result.Skipped++;
                    result.Errors.Add(Err(chunkFrom, "Answer",
                        $"No answer matched (chapter '{currentChapter ?? "?"}', Q{q.QuestionNumber}) — {Short(text)}"));
                    continue;
                }

                if (!existingTexts.Add(text)) { result.Duplicates++; continue; }

                var topic = await ResolveTopicAsync(topicCache, subject, chapter.Title, ct);

                var entity = new Question
                {
                    Text              = q.RequiresFigure ? text + FigureNote(q.FigureDescription) : text,
                    Subtopic          = string.IsNullOrWhiteSpace(q.Subtopic) ? chapter.Title : q.Subtopic.Trim(),
                    QuestionType      = q.IsNumerical ? QuestionType.NAT : QuestionType.MCQ,
                    Status            = options.PublishImmediately ? QuestionStatus.Published : QuestionStatus.Draft,
                    SubjectId         = subject.Id,
                    TopicId           = topic.Id,
                    DifficultyLevelId = masters.Difficulty(q.Category).Id,
                    ComplexityLevelId = masters.Complexity.Id,
                    MarksId           = masters.Marks.Id,
                    NegativeMarksId   = masters.NegativeMarks.Id,
                    CreatedBy         = options.ImportedBy,
                    UpdatedBy         = options.ImportedBy,
                };

                if (!ApplyAnswer(entity, q, answer, out var why))
                {
                    result.Skipped++;
                    result.Errors.Add(Err(chunkFrom, "Answer", $"Q{q.QuestionNumber}: {why} — {Short(text)}"));
                    continue;
                }

                _db.Questions.Add(entity);
                added.Add(entity);
                result.Imported++;
            }

            _logger.LogInformation("PYQ import: {File} pages {From}-{To} → {Count} question(s) so far.",
                fileName, chunkFrom, chunkTo, result.Imported);
        }

        if (added.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
            if (options.TestId.HasValue) await MapToTestAsync(added.Select(a => a.Id), options.TestId.Value, result, ct);
        }

        if (imageOptions > 0)
            result.Errors.Add(Err(0, "ImageOptions",
                $"{imageOptions} question(s) have picture options and were not imported — they need the image pipeline."));
        if (unmatched > 0)
            result.Errors.Add(Err(0, "Unmatched", $"{unmatched} question(s) could not be matched to an answer."));

        return result;
    }

    // ── Answers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies the key's answer. For multiple choice the key prints the option number (1-4);
    /// for a numerical question it prints the value. The model's is_numerical is cross-checked
    /// against the key rather than trusted: an answer larger than the option count can only be a
    /// value, and a question with options whose answer is 1-4 can only be a choice.
    /// </summary>
    private static bool ApplyAnswer(Question entity, Extracted q, string answer, out string why)
    {
        why = "";
        var options = (q.Options ?? []).Where(o => !string.IsNullOrWhiteSpace(o)).ToList();
        var isNumeric = decimal.TryParse(answer, System.Globalization.NumberStyles.Any,
                                         System.Globalization.CultureInfo.InvariantCulture, out var value);

        // A multiple-choice question that arrived without options must NOT fall through to the
        // numerical branch: the key's "3" means option 3, and saving it as the value 3 looks
        // perfectly normal while being wrong. Skip it and say so.
        if (!q.IsNumerical && options.Count < 2)
        {
            why = "multiple-choice question arrived without readable options";
            return false;
        }

        var looksNumerical = q.IsNumerical || (isNumeric && value > options.Count);

        if (looksNumerical)
        {
            if (!isNumeric) { why = $"answer '{answer}' is not a number"; return false; }
            entity.QuestionType    = QuestionType.NAT;
            entity.NumericalAnswer = value;
            entity.Options.Clear();
            return true;
        }

        if (!isNumeric || value < 1 || value > options.Count)
        {
            why = $"answer '{answer}' is not one of the {options.Count} options";
            return false;
        }

        var correct = (int)value - 1;
        entity.QuestionType = QuestionType.MCQ;
        for (var i = 0; i < options.Count; i++)
            entity.Options.Add(new QuestionOption
            {
                Label     = (char)('A' + i),
                Text      = options[i].Trim(),
                IsCorrect = i == correct,
                SortOrder = i,
            });

        return true;
    }

    /// <summary>Matches the chapter the model reported to a chapter in the answer key.</summary>
    private static PyqChapter? MatchChapter(List<PyqChapter> chapters, string? reported)
    {
        if (string.IsNullOrWhiteSpace(reported)) return null;

        var wanted = Normalise(reported);
        if (wanted.Length < 4) return null;

        return chapters.FirstOrDefault(c => Normalise(c.Title) == wanted)
            ?? chapters.FirstOrDefault(c => Normalise(c.Title).Contains(wanted, StringComparison.Ordinal))
            ?? chapters.FirstOrDefault(c => wanted.Contains(Normalise(c.Title), StringComparison.Ordinal));
    }

    private static string Normalise(string s) =>
        new(s.Replace("&", "and").Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    // ── Masters, topics, mapping ─────────────────────────────────────────────

    private sealed class Masters
    {
        public required List<DifficultyLevel> Difficulties { get; init; }
        public required ComplexityLevel       Complexity   { get; init; }
        public required MarksMaster           Marks        { get; init; }
        public required NegativeMarksMaster   NegativeMarks { get; init; }

        /// <summary>The book's own grouping is a human judgement — better than asking the model.</summary>
        public DifficultyLevel Difficulty(string? category)
        {
            var wanted = (category ?? "").ToLowerInvariant() switch
            {
                var c when c.Contains("concept")  => "Easy",
                var c when c.Contains("advanced") => "Hard",
                _                                  => "Medium",
            };
            return Difficulties.FirstOrDefault(d => d.Name.Equals(wanted, StringComparison.OrdinalIgnoreCase))
                ?? Difficulties.FirstOrDefault(d => d.Name.Equals("Medium", StringComparison.OrdinalIgnoreCase))
                ?? Difficulties[0];
        }
    }

    private async Task<Masters?> LoadMastersAsync(CancellationToken ct)
    {
        var difficulties = await _db.DifficultyLevels.AsNoTracking().ToListAsync(ct);
        var complexities = await _db.ComplexityLevels.AsNoTracking().ToListAsync(ct);
        var marks        = await _db.MarksMaster.AsNoTracking().ToListAsync(ct);
        var negMarks     = await _db.NegativeMarksMaster.AsNoTracking().ToListAsync(ct);

        if (difficulties.Count == 0 || complexities.Count == 0 || marks.Count == 0 || negMarks.Count == 0)
            return null;

        return new Masters
        {
            Difficulties  = difficulties,
            Complexity    = complexities.FirstOrDefault(c => c.Name == "Medium") ?? complexities[0],
            Marks         = marks.FirstOrDefault(m => m.Name == "4 Marks")       ?? marks[0],
            NegativeMarks = negMarks.FirstOrDefault(n => n.Name == "-1 Mark")    ?? negMarks[0],
        };
    }

    /// <summary>
    /// Finds the chapter's topic under this subject, creating it when missing — these books cover
    /// chapters outside the current syllabus list (States of Matter, Polymers, …).
    /// </summary>
    private async Task<Topic> ResolveTopicAsync(
        Dictionary<string, Topic> cache, Subject subject, string chapterTitle, CancellationToken ct)
    {
        if (cache.TryGetValue(chapterTitle, out var cached)) return cached;

        var existing = (await _db.Topics.Where(t => t.SubjectId == subject.Id).ToListAsync(ct))
            .FirstOrDefault(t => Normalise(t.Name) == Normalise(chapterTitle));

        if (existing is null)
        {
            existing = new Topic { Name = chapterTitle, SubjectId = subject.Id, SortOrder = 200 };
            _db.Topics.Add(existing);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("PYQ import: created topic '{Topic}' under {Subject}.", chapterTitle, subject.Name);
        }

        cache[chapterTitle] = existing;
        return existing;
    }

    private async Task MapToTestAsync(IEnumerable<Guid> questionIds, Guid testId, ImportResultDto result, CancellationToken ct)
    {
        var test = await _db.Tests.AsNoTracking().FirstOrDefaultAsync(t => t.Id == testId, ct);
        if (test is null) return;

        var existing = (await _db.TestQuestions.Where(tq => tq.TestId == testId)
            .Select(tq => tq.QuestionId).ToListAsync(ct)).ToHashSet();

        var order = existing.Count + 1;
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

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Extracts a page range as a small PDF — the book itself is far too large to send.</summary>
    private static byte[] Slice(byte[] pdfBytes, int from, int to)
    {
        using var doc = PdfDocument.Open(pdfBytes);
        var builder = new PdfDocumentBuilder();
        for (var p = from; p <= to && p <= doc.NumberOfPages; p++) builder.AddPage(doc, p);
        return builder.Build();
    }

    private static string FigureNote(string? description) =>
        FigureNotePrefix +
        System.Net.WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(description)
                ? "add the diagram from the source book before publishing."
                : "draw and insert this diagram before publishing: " + description.Trim()) +
        "]</em></p>";

    private static ImportRowError Err(int row, string field, string message)
        => new() { Row = row, Field = field, Message = message };

    private static string Short(string text) => text.Length <= 60 ? text : text[..60] + "…";

    private sealed class Extracted
    {
        public int           QuestionNumber    { get; init; }
        public string?       ChapterTitle      { get; init; }
        public string?       Subtopic          { get; init; }
        public string?       Category          { get; init; }
        public string?       QuestionText      { get; init; }
        public bool          IsNumerical       { get; init; }
        public List<string>? Options           { get; init; }
        public bool          OptionsAreImages  { get; init; }
        public bool          RequiresFigure    { get; init; }
        public string?       FigureDescription { get; init; }
    }

    private static List<Extracted> Parse(string raw)
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
                   ?? throw new InvalidOperationException("response was not a list of questions");

        var list = new List<Extracted>();
        foreach (var item in arr)
        {
            if (item is not JsonObject o) continue;
            try
            {
                list.Add(new Extracted
                {
                    QuestionNumber    = int.TryParse(o["question_number"]?.ToString(), out var n) ? n : 0,
                    ChapterTitle      = o["chapter_title"]?.GetValue<string>(),
                    Subtopic          = o["subtopic"]?.GetValue<string>(),
                    Category          = o["category"]?.GetValue<string>(),
                    QuestionText      = o["question_text"]?.GetValue<string>(),
                    IsNumerical       = o["is_numerical"]?.GetValue<bool>() ?? false,
                    Options           = (o["options"] as JsonArray)?.Select(x => x?.ToString() ?? "").ToList(),
                    OptionsAreImages  = o["options_are_images"]?.GetValue<bool>() ?? false,
                    RequiresFigure    = o["requires_figure"]?.GetValue<bool>() ?? false,
                    FigureDescription = o["figure_description"]?.GetValue<string>(),
                });
            }
            catch { /* skip one malformed item rather than losing the chunk */ }
        }
        return list;
    }
}
