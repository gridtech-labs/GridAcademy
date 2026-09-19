using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace GridAcademy.Services.PyqBook;

/// <summary>One chapter of a PYQ book, as listed in the book's answer key.</summary>
public sealed class PyqChapter
{
    public int    Number { get; init; }
    public string Title  { get; init; } = "";

    /// <summary>Question number → answer exactly as printed: "3" (an option) or "225" (a value).</summary>
    public Dictionary<int, string> Answers { get; init; } = [];
}

/// <summary>What could be read from a PYQ book without a model.</summary>
public sealed class PyqBookInfo
{
    public int PageCount { get; init; }

    /// <summary>Pages whose text could be extracted — a health check on the PDF.</summary>
    public int PagesWithText { get; init; }

    /// <summary>First page of the answer-key section (PageCount + 1 when there is none).</summary>
    public int AnswerKeyStartPage { get; init; }

    public List<PyqChapter> Chapters { get; init; } = [];
}

/// <summary>
/// Reads what a "previous years' questions" book (MathonGo PYQ and similar) states in plain text,
/// so the model is never asked for something a machine can read exactly.
///
/// Scope, learned from the 493-page JEE Main Chemistry book (33 chapters, 4,115 answers):
///
///   • The ANSWER KEY is parsed here. It is a text section at the end, grouped by chapter,
///     listing "Q1 (225) Q2 (4) …", with question numbers restarting at 1 in every chapter.
///     This parses exactly, so no answer is ever guessed.
///
///   • CHAPTER BOUNDARIES are NOT parsed here. They cannot be read reliably from this PDF's text
///     layer: chapter dividers ("CHAPTER 27") and many question numbers are drawn as graphics —
///     PdfPig extracts none of the 33 dividers — and several chapters print no running header at
///     all, so neither dividers, headers nor numbering gaps give a dependable boundary.
///     The extraction pass reads each page as an image and reports the chapter it can see, which
///     is carried forward across pages that show none.
/// </summary>
public static class PyqBookStructure
{
    /// <summary>A page listing this many "Qn (answer)" pairs belongs to the answer-key section.</summary>
    private const int KeyPairsPerPage = 20;

    private static readonly Regex KeyPairRx = new(@"Q(\d{1,4})\s*\(([^)]*)\)", RegexOptions.Compiled);

    /// <summary>
    /// A chapter heading inside the answer key: "12. Surface Chemistry" immediately followed by
    /// that chapter's first answer. Matched on a flattened stream rather than per line, because
    /// PdfPig returns a page as one continuous string with no newlines. Titles may contain
    /// brackets ("Thermodynamics (C)", "p Block Elements (Group 13 &amp; 14)"), so the title is
    /// bounded by the lookahead to "Q1 (" rather than by punctuation.
    /// </summary>
    private static readonly Regex KeyHeadingRx =
        new(@"(?<num>\d{1,2})\.\s*(?<title>[A-Za-z][^\r\n]{2,70}?)\s*(?=Q1\s*\()", RegexOptions.Compiled);

    public static PyqBookInfo Read(byte[] pdfBytes)
    {
        using var doc = PdfDocument.Open(pdfBytes);
        var pageCount = doc.NumberOfPages;

        var text = new string[pageCount + 1];               // 1-based
        var withText = 0;
        for (var p = 1; p <= pageCount; p++)
        {
            text[p] = doc.GetPage(p).Text ?? "";
            if (text[p].Length > 50) withText++;
        }

        var keyStart = FindAnswerKeyStart(text, pageCount);

        return new PyqBookInfo
        {
            PageCount          = pageCount,
            PagesWithText      = withText,
            AnswerKeyStartPage = keyStart,
            Chapters           = ParseAnswerKey(text, keyStart, pageCount),
        };
    }

    /// <summary>
    /// First page of the answer-key section: the first page dense with "Qn (answer)" pairs.
    /// Matching the words "Answer Keys" instead hits the contents page.
    /// </summary>
    private static int FindAnswerKeyStart(string[] text, int pageCount)
    {
        for (var p = 1; p <= pageCount; p++)
            if (KeyPairRx.Matches(text[p]).Count >= KeyPairsPerPage)
                return p;

        return pageCount + 1;   // no key section — answers stay empty
    }

    private static List<PyqChapter> ParseAnswerKey(string[] text, int keyStart, int pageCount)
    {
        var chapters = new List<PyqChapter>();
        if (keyStart > pageCount) return chapters;

        // One stream: a chapter's answers frequently run across a page break.
        var keyText = string.Join(" ", Enumerable.Range(keyStart, pageCount - keyStart + 1)
            .Select(p => text[p]));

        var headings = KeyHeadingRx.Matches(keyText);
        for (var h = 0; h < headings.Count; h++)
        {
            var heading = headings[h];
            var from    = heading.Index + heading.Length;
            var to      = h + 1 < headings.Count ? headings[h + 1].Index : keyText.Length;

            var chapter = new PyqChapter
            {
                Number = int.Parse(heading.Groups["num"].Value),
                Title  = CleanTitle(heading.Groups["title"].Value),
            };

            foreach (Match m in KeyPairRx.Matches(keyText[from..to]))
                chapter.Answers[int.Parse(m.Groups[1].Value)] = m.Groups[2].Value.Trim();

            if (chapter.Answers.Count > 0) chapters.Add(chapter);
        }

        return chapters;
    }

    /// <summary>Trims stray leaders the flattened text can attach to a heading.</summary>
    private static string CleanTitle(string raw) =>
        Regex.Replace(raw, @"^[\s\.\-–—]+|[\s\.\-–—]+$", "").Trim();

    /// <summary>
    /// Page ranges to send to the model, in reading order. Chunks stay small: a whole chapter in
    /// one request is a multi-megabyte upload and a long single generation, and a 141 MB book
    /// cannot be sent at all.
    /// </summary>
    public static IEnumerable<(int From, int To)> Chunks(int firstPage, int lastPage, int pagesPerChunk)
    {
        for (var p = firstPage; p <= lastPage; p += pagesPerChunk)
            yield return (p, Math.Min(p + pagesPerChunk - 1, lastPage));
    }
}
