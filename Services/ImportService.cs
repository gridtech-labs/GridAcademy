using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using GridAcademy.Data;
using GridAcademy.Data.Entities.Content;
using GridAcademy.DTOs.Content.Import;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace GridAcademy.Services;

public class ImportService : IImportService
{
    private readonly AppDbContext            _db;
    private readonly ILogger<ImportService>  _logger;
    private readonly IMathpixService         _mathpix;

    public ImportService(AppDbContext db, ILogger<ImportService> logger, IMathpixService mathpix)
    {
        _db      = db;
        _logger  = logger;
        _mathpix = mathpix;
    }

    // ════════════════════════════════════════════════════════════════════════
    // CSV IMPORT
    // ════════════════════════════════════════════════════════════════════════

    public async Task<ImportResultDto> ImportCsvAsync(Stream stream, Guid? importedBy = null)
    {
        var result = new ImportResultDto { Source = "CSV" };
        var masters = await LoadMastersAsync();

        using var reader = new StreamReader(stream);
        using var csv    = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord    = true,
            TrimOptions        = TrimOptions.Trim,
            MissingFieldFound  = null
        });

        var rows = new List<ImportRow>();
        try
        {
            rows = csv.GetRecords<ImportRow>().ToList();
        }
        catch (Exception ex)
        {
            result.Errors.Add(new ImportRowError { Row = 0, Field = "File", Message = $"CSV parse error: {ex.Message}" });
            return result;
        }

        result.TotalRows = rows.Count;
        int rowNum = 2; // header is row 1

        foreach (var row in rows)
        {
            var errs = ValidateAndImportRow(row, rowNum, masters, importedBy);
            if (errs.Count > 0) { result.Errors.AddRange(errs); result.Skipped++; }
            else result.Imported++;
            rowNum++;
        }

        if (result.Imported > 0) await _db.SaveChangesAsync();
        return result;
    }

    // ════════════════════════════════════════════════════════════════════════
    // EXCEL IMPORT
    // ════════════════════════════════════════════════════════════════════════

    public async Task<ImportResultDto> ImportExcelAsync(Stream stream, Guid? importedBy = null)
    {
        var result  = new ImportResultDto { Source = "Excel" };
        var masters = await LoadMastersAsync();

        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.First();

        var rows = new List<ImportRow>();
        int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int r = 2; r <= lastRow; r++) // row 1 = header
        {
            var row = new ImportRow
            {
                SubjectName      = ws.Cell(r, 1).GetString(),
                TopicName        = ws.Cell(r, 2).GetString(),
                Subtopic         = ws.Cell(r, 3).GetString(),
                DifficultyLevel  = ws.Cell(r, 4).GetString(),
                ComplexityLevel  = ws.Cell(r, 5).GetString(),
                ExamType         = ws.Cell(r, 6).GetString(),
                Marks            = ws.Cell(r, 7).GetString(),
                NegativeMarks    = ws.Cell(r, 8).GetString(),
                QuestionType     = ws.Cell(r, 9).GetString(),
                QuestionText     = ws.Cell(r, 10).GetString(),
                OptionA          = ws.Cell(r, 11).GetString(),
                OptionB          = ws.Cell(r, 12).GetString(),
                OptionC          = ws.Cell(r, 13).GetString(),
                OptionD          = ws.Cell(r, 14).GetString(),
                CorrectOptions   = ws.Cell(r, 15).GetString(),
                NumericalAnswer  = ws.Cell(r, 16).GetString(),
                Tags             = ws.Cell(r, 17).GetString(),
                Solution         = ws.Cell(r, 18).GetString()
            };
            rows.Add(row);
        }

        result.TotalRows = rows.Count;
        int rowNum = 2;

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.QuestionText)) { result.Skipped++; rowNum++; continue; }
            var errs = ValidateAndImportRow(row, rowNum, masters, importedBy);
            if (errs.Count > 0) { result.Errors.AddRange(errs); result.Skipped++; }
            else result.Imported++;
            rowNum++;
        }

        if (result.Imported > 0) await _db.SaveChangesAsync();
        return result;
    }

    // ════════════════════════════════════════════════════════════════════════
    // PDF IMPORT (JEE / NEET pattern)
    // ════════════════════════════════════════════════════════════════════════

    public async Task<ImportResultDto> ImportPdfAsync(Stream stream, Guid? importedBy = null)
    {
        var result  = new ImportResultDto { Source = "PDF" };
        var masters = await LoadMastersAsync();

        string fullText;
        using (var doc = PdfDocument.Open(stream))
        {
            var sb = new System.Text.StringBuilder();
            foreach (var page in doc.GetPages())
                sb.AppendLine(page.Text);
            fullText = sb.ToString();
        }

        var questions = ParseJeePdf(fullText, masters.Subjects);
        result.TotalRows = questions.Count;

        // All PDF imports use first available masters as defaults
        var defaultSubject = masters.Subjects.FirstOrDefault();
        var defaultTopic   = masters.Topics.FirstOrDefault();
        var defaultDiff    = masters.DifficultyLevels.FirstOrDefault();
        var defaultComp    = masters.ComplexityLevels.FirstOrDefault();
        var defaultExam    = masters.ExamTypes.FirstOrDefault();
        var defaultMarks   = masters.Marks.FirstOrDefault();
        var defaultNegMark = masters.NegativeMarks.FirstOrDefault();

        if (defaultSubject == null || defaultTopic == null || defaultDiff == null ||
            defaultComp == null || defaultExam == null || defaultMarks == null || defaultNegMark == null)
        {
            result.Errors.Add(new ImportRowError
            {
                Row = 0, Field = "Masters",
                Message = "Master data not seeded. Run the application first to seed subjects, topics, and other master tables."
            });
            return result;
        }

        int rowNum = 1;
        foreach (var pq in questions)
        {
            try
            {
                var entity = new Question
                {
                    Text              = pq.Text,
                    Status            = QuestionStatus.Draft,
                    QuestionType      = pq.Type,
                    SubjectId         = pq.SubjectId ?? defaultSubject.Id,
                    TopicId           = pq.TopicId ?? defaultTopic.Id,
                    DifficultyLevelId = defaultDiff.Id,
                    ComplexityLevelId = defaultComp.Id,
                    MarksId           = pq.MarksId ?? defaultMarks.Id,
                    NegativeMarksId   = pq.NegMarksId ?? defaultNegMark.Id,
                    ExamTypeId        = pq.ExamTypeId ?? defaultExam.Id,
                    NumericalAnswer   = pq.NumericalAnswer,
                    CreatedBy         = importedBy,
                    UpdatedBy         = importedBy
                };

                foreach (var opt in pq.Options)
                    entity.Options.Add(opt);

                _db.Questions.Add(entity);
                result.Imported++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportRowError { Row = rowNum, Field = "Parse", Message = ex.Message });
                result.Skipped++;
            }
            rowNum++;
        }

        if (result.Imported > 0) await _db.SaveChangesAsync();
        return result;
    }

    // ════════════════════════════════════════════════════════════════════════
    // PDF IMPORT — OCR path via Mathpix API
    // ════════════════════════════════════════════════════════════════════════

    public async Task<ImportResultDto> ImportPdfOcrAsync(Stream stream, Guid? importedBy = null)
    {
        var result  = new ImportResultDto { Source = "PDF (Mathpix OCR)" };
        var masters = await LoadMastersAsync();

        // ── Read PDF bytes (Mathpix requires the whole file in one request) ──
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var pdfBytes = ms.ToArray();

        // ── Run OCR ──────────────────────────────────────────────────────────
        string mmd;
        try
        {
            mmd = await _mathpix.OcrPdfAsync(pdfBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mathpix OCR failed");
            result.Errors.Add(new ImportRowError
            {
                Row     = 0,
                Field   = "Mathpix OCR",
                Message = ex.Message
            });
            return result;
        }

        // ── Parse MMD into question objects ───────────────────────────────────
        var questions = ParseMathpixMmd(mmd, masters.Subjects);
        result.TotalRows = questions.Count;

        // ── Save ──────────────────────────────────────────────────────────────
        var defaultSubject = masters.Subjects.FirstOrDefault();
        var defaultTopic   = masters.Topics.FirstOrDefault();
        var defaultDiff    = masters.DifficultyLevels.FirstOrDefault();
        var defaultComp    = masters.ComplexityLevels.FirstOrDefault();
        var defaultExam    = masters.ExamTypes.FirstOrDefault();
        var defaultMarks   = masters.Marks.FirstOrDefault();
        var defaultNegMark = masters.NegativeMarks.FirstOrDefault();

        if (defaultSubject == null || defaultTopic == null || defaultDiff == null ||
            defaultComp == null || defaultExam == null || defaultMarks == null || defaultNegMark == null)
        {
            result.Errors.Add(new ImportRowError
            {
                Row = 0, Field = "Masters",
                Message = "Master data not seeded. Run the application first to seed subjects, topics, and other master tables."
            });
            return result;
        }

        int rowNum = 1;
        foreach (var pq in questions)
        {
            try
            {
                var entity = new Question
                {
                    Text              = pq.Text,
                    Status            = QuestionStatus.Draft,
                    QuestionType      = pq.Type,
                    SubjectId         = pq.SubjectId  ?? defaultSubject.Id,
                    TopicId           = pq.TopicId    ?? defaultTopic.Id,
                    DifficultyLevelId = defaultDiff.Id,
                    ComplexityLevelId = defaultComp.Id,
                    MarksId           = pq.MarksId    ?? defaultMarks.Id,
                    NegativeMarksId   = pq.NegMarksId ?? defaultNegMark.Id,
                    ExamTypeId        = pq.ExamTypeId ?? defaultExam.Id,
                    NumericalAnswer   = pq.NumericalAnswer,
                    CreatedBy         = importedBy,
                    UpdatedBy         = importedBy
                };

                foreach (var opt in pq.Options)
                    entity.Options.Add(opt);

                _db.Questions.Add(entity);
                result.Imported++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportRowError { Row = rowNum, Field = "Parse", Message = ex.Message });
                result.Skipped++;
            }
            rowNum++;
        }

        if (result.Imported > 0) await _db.SaveChangesAsync();
        return result;
    }

    // ════════════════════════════════════════════════════════════════════════
    // MATHPIX MMD PARSER
    // Mathpix returns Markdown with LaTeX math in $…$ / $$…$$ notation.
    // We strip the Markdown markup, run the same JEE parser for structure,
    // then convert every $…$ expression to a Quill ql-formula <span>.
    // ════════════════════════════════════════════════════════════════════════

    private static List<ParsedQuestion> ParseMathpixMmd(string mmd, IReadOnlyList<Subject> subjects)
    {
        // ── 1. Strip Markdown formatting ─────────────────────────────────────
        var text = mmd;

        // ## / ### headings → plain text on its own line (subject detection reuses these)
        text = Regex.Replace(text, @"^#{1,4}\s+(.+)$", "\n$1\n", RegexOptions.Multiline);

        // **bold** → plain
        text = Regex.Replace(text, @"\*\*(.+?)\*\*", "$1", RegexOptions.Singleline);

        // *italic* → plain
        text = Regex.Replace(text, @"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", "$1", RegexOptions.Singleline);

        // Mathpix sometimes uses \( \) / \[ \] for math — normalise to $ $
        text = Regex.Replace(text, @"\\\[(.+?)\\\]", "$$$$1$$", RegexOptions.Singleline);
        text = Regex.Replace(text, @"\\\((.+?)\\\)", "$$$1$$", RegexOptions.Singleline);

        // ── 2. Reuse the existing JEE structure parser ───────────────────────
        var questions = ParseJeePdf(text, subjects);

        // ── 3. Convert $LaTeX$ to Quill ql-formula HTML spans ────────────────
        foreach (var q in questions)
        {
            q.Text = WrapMathAsQuillHtml(q.Text);
            foreach (var opt in q.Options)
                opt.Text = WrapMathInline(opt.Text);
        }

        return questions;
    }

    // Wraps question text in <p>…</p> and converts all $math$ spans
    private static string WrapMathAsQuillHtml(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        var html = WrapMathInline(text.Trim());
        return html.StartsWith("<p>", StringComparison.OrdinalIgnoreCase) ? html : $"<p>{html}</p>";
    }

    // Converts $…$ (inline) and $$…$$ (display) to Quill ql-formula spans
    private static string WrapMathInline(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        // Display math $$…$$ first (must precede inline pass)
        text = Regex.Replace(text, @"\$\$(.+?)\$\$",
            m => QuillFormula(m.Groups[1].Value.Trim()),
            RegexOptions.Singleline);

        // Inline math $…$  (non-greedy, won't cross line-breaks or another $)
        text = Regex.Replace(text, @"\$([^\$\r\n]{1,500}?)\$",
            m => QuillFormula(m.Groups[1].Value.Trim()));

        return text;
    }

    // Builds a Quill formula embed span with HTML-encoded LaTeX in data-value
    private static string QuillFormula(string latex)
        => $"<span class=\"ql-formula\" data-value=\"{WebUtility.HtmlEncode(latex)}\"></span>";

    // ════════════════════════════════════════════════════════════════════════
    // PDF PARSER — JEE / NEET / general exam pattern
    // ════════════════════════════════════════════════════════════════════════

    // Known subject heading words (must match names seeded in SubjectMaster)
    private static readonly string[] KnownSubjectHeadings =
        ["Mathematics", "Physics", "Chemistry", "Biology"];

    private static List<ParsedQuestion> ParseJeePdf(string text, IReadOnlyList<Subject> subjects)
    {
        var questions = new List<ParsedQuestion>();

        // ── 1. Normalize ─────────────────────────────────────────────────────
        text = Regex.Replace(text, @"\r\n|\r", "\n");
        text = Regex.Replace(text, @"[ \t]+", " ");

        // ── 2. Strip exam paper page headers ─────────────────────────────────
        // e.g. "JEE (Advanced) 2022   Paper 2  1/8"
        text = Regex.Replace(text,
            @"(?:JEE|NEET|CUET|BITSAT)[^\n]*?\d+\s*/\s*\d+\s*",
            "\n", RegexOptions.IgnoreCase);

        // ── 3. Insert newlines before Q.N markers ────────────────────────────
        // PdfPig often returns each page as a single line, so Q.5 appears
        // in the middle of text without a preceding newline.
        text = Regex.Replace(text, @"(?<!\n)(Q\.?\s*\d+\s)", "\n$1");

        // ── 4. Insert newlines before subject headings ───────────────────────
        // Ensures "...previous text MATHEMATICS SECTION 1..." becomes
        // "...previous text\nMATHEMATICS\nSECTION 1..."
        // so they're detectable as standalone lines later.
        var subjectAlteration = string.Join("|",
            KnownSubjectHeadings.Select(Regex.Escape));
        text = Regex.Replace(text,
            $@"(?<!\n)\s*\b({subjectAlteration})\b\s*",
            "\n$1\n",
            RegexOptions.IgnoreCase);

        // ── 5. Strip section description blocks ──────────────────────────────
        // Matches "SECTION N (Maximum marks:..." up to (but not including) Q.N
        text = Regex.Replace(text,
            @"SECTION\s+\d+\s*\(Maximum\s+marks[^Q]{0,800}",
            "\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // ── 6. Build subject lookup: name → DB id ────────────────────────────
        var subjectLookup = subjects.ToDictionary(
            s => s.Name,
            s => s.Id,
            StringComparer.OrdinalIgnoreCase);

        // ── 7. Record all subject heading positions in the cleaned text ───────
        var subjectHeadingRx = new Regex(
            $@"(?m)^\s*({subjectAlteration})\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        var subjectPositions = subjectHeadingRx.Matches(text)
            .Cast<Match>()
            .Select(m => (Index: m.Index, Name: m.Groups[1].Value.Trim()))
            .OrderBy(x => x.Index)
            .ToList();

        // Helper: which subject id precedes text position `pos`?
        int? SubjectIdBefore(int pos)
        {
            var name = subjectPositions
                .Where(sp => sp.Index < pos)
                .OrderByDescending(sp => sp.Index)
                .Select(sp => sp.Name)
                .FirstOrDefault();
            return name != null && subjectLookup.TryGetValue(name, out var id) ? id : null;
        }

        // ── 8. Split on Q.N markers and build question list ──────────────────
        var qPattern = new Regex(@"(?m)^\s*Q\.?\s*(\d+)\s+", RegexOptions.Multiline);
        var matches  = qPattern.Matches(text);

        for (int i = 0; i < matches.Count; i++)
        {
            var qStart = matches[i].Index;
            var start  = qStart + matches[i].Length;
            var end    = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
            var chunk  = text[start..end];

            // Strip section header / end-of-paper fragments at chunk tail
            chunk = Regex.Replace(chunk,
                @"\s*(?:SECTION\s+\d+|END\s+OF\s+THE\s+QUESTION\s+PAPER|• This section)[^\n]*$",
                "", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            // Strip standalone subject headings at chunk end
            chunk = Regex.Replace(chunk,
                $@"\n\s*(?:{subjectAlteration})\s*$",
                "", RegexOptions.IgnoreCase);

            chunk = chunk.Trim();
            if (string.IsNullOrWhiteSpace(chunk) || chunk.Length < 15) continue;

            var pq = ParseQuestionChunk(chunk);
            if (pq != null)
            {
                pq.SubjectId = SubjectIdBefore(qStart);
                questions.Add(pq);
            }
        }

        return questions;
    }

    private static ParsedQuestion? ParseQuestionChunk(string chunk)
    {
        // Find options (A) (B) (C) (D) — handles both inline and multi-line formats
        var optPattern = new Regex(
            @"\(([ABCD])\)\s+(.+?)(?=\s*\([ABCD]\)|\s*$)",
            RegexOptions.Singleline);

        var optMatches = optPattern.Matches(chunk);
        var optStartIndex = optMatches.Count > 0 ? optMatches[0].Index : chunk.Length;
        var questionText  = chunk[..optStartIndex].Trim();

        // Strip trailing fill-in-blank underscores from NAT questions
        questionText = Regex.Replace(questionText, @"\s*_+\.?\s*$", "").Trim();
        questionText = Regex.Replace(questionText, @"\s+_+\s+", " ").Trim();

        if (string.IsNullOrWhiteSpace(questionText) || questionText.Length < 10) return null;

        var pq = new ParsedQuestion
        {
            Text = questionText,
            Type = optMatches.Count >= 2 ? QuestionType.MCQ : QuestionType.NAT
        };

        int sortOrder = 0;
        foreach (Match m in optMatches)
        {
            var label   = m.Groups[1].Value[0];
            var optText = m.Groups[2].Value.Trim();

            // Strip any section header / end-of-paper text that bled into the last option
            optText = Regex.Replace(optText,
                @"\s*(?:SECTION\s+\d+|END\s+OF\s+THE\s+QUESTION\s+PAPER).*$",
                "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();

            if (!string.IsNullOrWhiteSpace(optText))
                pq.Options.Add(new QuestionOption
                {
                    Label     = label,
                    Text      = optText,
                    SortOrder = sortOrder++
                });
        }

        return pq;
    }

    // ════════════════════════════════════════════════════════════════════════
    // ROW VALIDATION & QUESTION CREATION (shared by CSV and Excel paths)
    // ════════════════════════════════════════════════════════════════════════

    private List<ImportRowError> ValidateAndImportRow(ImportRow row, int rowNum, MasterCache m, Guid? createdBy)
    {
        var errors = new List<ImportRowError>();

        if (string.IsNullOrWhiteSpace(row.QuestionText))
        { errors.Add(new ImportRowError { Row = rowNum, Field = "QuestionText", Message = "Required" }); return errors; }

        // Resolve FKs by name
        var subject = m.Subjects.FirstOrDefault(x => x.Name.Equals(row.SubjectName?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (subject == null) { errors.Add(new ImportRowError { Row = rowNum, Field = "SubjectName", Message = $"'{row.SubjectName}' not found" }); }

        var topic = m.Topics.FirstOrDefault(x => x.Name.Equals(row.TopicName?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (topic == null) { errors.Add(new ImportRowError { Row = rowNum, Field = "TopicName", Message = $"'{row.TopicName}' not found" }); }

        var diff = m.DifficultyLevels.FirstOrDefault(x => x.Name.Equals(row.DifficultyLevel?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (diff == null) { errors.Add(new ImportRowError { Row = rowNum, Field = "DifficultyLevel", Message = $"'{row.DifficultyLevel}' not found" }); }

        var comp = m.ComplexityLevels.FirstOrDefault(x => x.Name.Equals(row.ComplexityLevel?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (comp == null) { errors.Add(new ImportRowError { Row = rowNum, Field = "ComplexityLevel", Message = $"'{row.ComplexityLevel}' not found" }); }

        var exam = m.ExamTypes.FirstOrDefault(x => x.Name.Equals(row.ExamType?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (exam == null) { errors.Add(new ImportRowError { Row = rowNum, Field = "ExamType", Message = $"'{row.ExamType}' not found" }); }

        var marks = m.Marks.FirstOrDefault(x => x.Name.Equals(row.Marks?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (marks == null) { errors.Add(new ImportRowError { Row = rowNum, Field = "Marks", Message = $"'{row.Marks}' not found" }); }

        var negMarks = m.NegativeMarks.FirstOrDefault(x => x.Name.Equals(row.NegativeMarks?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (negMarks == null) { errors.Add(new ImportRowError { Row = rowNum, Field = "NegativeMarks", Message = $"'{row.NegativeMarks}' not found" }); }

        if (!Enum.TryParse<QuestionType>(row.QuestionType?.Trim(), true, out var qType))
        { errors.Add(new ImportRowError { Row = rowNum, Field = "QuestionType", Message = $"'{row.QuestionType}' is invalid. Use: MCQ, MSQ, NAT, FillInBlanks, TrueFalse, MatchTheFollowing, AssertionReason, PassageBased, MatrixMatch" }); }

        if (errors.Count > 0) return errors;

        // Build entity
        var entity = new Question
        {
            Text              = row.QuestionText.Trim(),
            Solution          = row.Solution?.Trim(),
            Subtopic          = row.Subtopic?.Trim(),
            QuestionType      = qType,
            Status            = QuestionStatus.Draft,
            SubjectId         = subject!.Id,
            TopicId           = topic!.Id,
            DifficultyLevelId = diff!.Id,
            ComplexityLevelId = comp!.Id,
            MarksId           = marks!.Id,
            NegativeMarksId   = negMarks!.Id,
            ExamTypeId        = exam!.Id,
            CreatedBy         = createdBy,
            UpdatedBy         = createdBy
        };

        if (qType == QuestionType.NAT)
        {
            if (!decimal.TryParse(row.NumericalAnswer, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
            { errors.Add(new ImportRowError { Row = rowNum, Field = "NumericalAnswer", Message = "Must be a number for Numerical questions" }); return errors; }
            entity.NumericalAnswer = num;
        }
        else
        {
            var correct = (row.CorrectOptions ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                           .Select(x => x.ToUpper()).ToHashSet();

            AddOption(entity, 'A', row.OptionA, correct);
            AddOption(entity, 'B', row.OptionB, correct);
            AddOption(entity, 'C', row.OptionC, correct);
            AddOption(entity, 'D', row.OptionD, correct);
        }

        // Tags (comma-separated names, created if missing)
        if (!string.IsNullOrWhiteSpace(row.Tags))
        {
            foreach (var tagName in row.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var tag = m.Tags.FirstOrDefault(x => x.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
                if (tag != null)
                    entity.QuestionTags.Add(new QuestionTag { TagId = tag.Id });
            }
        }

        _db.Questions.Add(entity);
        return errors;
    }

    private static void AddOption(Question entity, char label, string? text, HashSet<string> correct)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        entity.Options.Add(new QuestionOption
        {
            Label     = label,
            Text      = text.Trim(),
            IsCorrect = correct.Contains(label.ToString())
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    // MASTER DATA CACHE — load once per import call
    // ════════════════════════════════════════════════════════════════════════

    private async Task<MasterCache> LoadMastersAsync() => new()
    {
        Subjects        = await _db.Subjects.AsNoTracking().ToListAsync(),
        Topics          = await _db.Topics.AsNoTracking().ToListAsync(),
        DifficultyLevels = await _db.DifficultyLevels.AsNoTracking().ToListAsync(),
        ComplexityLevels = await _db.ComplexityLevels.AsNoTracking().ToListAsync(),
        ExamTypes        = await _db.ExamTypes.AsNoTracking().ToListAsync(),
        Marks            = await _db.MarksMaster.AsNoTracking().ToListAsync(),
        NegativeMarks    = await _db.NegativeMarksMaster.AsNoTracking().ToListAsync(),
        Tags             = await _db.Tags.AsNoTracking().ToListAsync()
    };

    private record MasterCache
    {
        public List<Subject>              Subjects         { get; init; } = [];
        public List<Topic>                Topics           { get; init; } = [];
        public List<DifficultyLevel>      DifficultyLevels { get; init; } = [];
        public List<ComplexityLevel>      ComplexityLevels { get; init; } = [];
        public List<ExamType>             ExamTypes        { get; init; } = [];
        public List<MarksMaster>          Marks            { get; init; } = [];
        public List<NegativeMarksMaster>  NegativeMarks    { get; init; } = [];
        public List<Tag>                  Tags             { get; init; } = [];
    }

    // Internal CSV row model
    private class ImportRow
    {
        public string? SubjectName     { get; set; }
        public string? TopicName       { get; set; }
        public string? Subtopic        { get; set; }
        public string? DifficultyLevel { get; set; }
        public string? ComplexityLevel { get; set; }
        public string? ExamType        { get; set; }
        public string? Marks           { get; set; }
        public string? NegativeMarks   { get; set; }
        public string? QuestionType    { get; set; }
        public string? QuestionText    { get; set; }
        public string? OptionA         { get; set; }
        public string? OptionB         { get; set; }
        public string? OptionC         { get; set; }
        public string? OptionD         { get; set; }
        public string? CorrectOptions  { get; set; }
        public string? NumericalAnswer { get; set; }
        public string? Tags            { get; set; }
        public string? Solution        { get; set; }
    }

    // Minimal parsed question from PDF
    private class ParsedQuestion
    {
        public string              Text            { get; set; } = "";
        public QuestionType        Type            { get; set; }
        public decimal?            NumericalAnswer { get; set; }
        public List<QuestionOption> Options        { get; set; } = [];
        public int?                SubjectId       { get; set; }
        public int?                TopicId         { get; set; }
        public int?                ExamTypeId      { get; set; }
        public int?                MarksId         { get; set; }
        public int?                NegMarksId      { get; set; }
    }
}
