namespace GridAcademy.Helpers;

/// <summary>
/// The standard instructions shown before every test, for every exam.
///
/// Single source of truth: the admin Create/Edit pages pre-fill it, TestService applies it
/// whenever a test is saved without instructions, and DbSeeder backfills tests that have none
/// or still carry an older version of this text. Change the wording here and the next deploy
/// updates every test that has not been customised — no editing tests by hand.
///
/// Deliberately exam-agnostic (no JEE/NEET-specific marks) and written to COMPLEMENT the
/// instructions screen, which already renders the question-palette colour legend, the
/// duration, the question count, the pass mark and a per-section marks table.
///
/// Every statement matches how the exam engine actually behaves: the timer runs server-side
/// from the moment the attempt starts, numerical answers carry no negative marking whatever
/// the section says, and tab switches are recorded and shown on the result.
///
/// IMPORTANT — the generated HTML is a SINGLE LINE with no indentation. The admin editor is
/// Quill, whose editing surface uses `white-space: pre-wrap`: any line breaks and leading
/// spaces in the source would be preserved there and push the box into a horizontal scroll.
/// Keep each item as one string below; the HTML is assembled without newlines.
/// </summary>
public static class DefaultTestInstructions
{
    /// <summary>Marks older copies that shipped with an unreplaced "[Duration]" placeholder.</summary>
    public const string LegacyPlaceholder = "[Duration]";

    /// <summary>
    /// Identifies text that came from here, so the seeder can upgrade old copies. Bump the
    /// version whenever the wording changes. Quill drops HTML comments, so a copy an admin has
    /// edited loses this marker and is then treated as custom and never overwritten.
    /// </summary>
    public const string MarkerPrefix = "gridacademy-default-instructions";
    private const string Marker = $"<!--{MarkerPrefix}-v2-->";

    /// <summary>Wording of the first version, so copies already saved can be recognised.</summary>
    public const string V1Phrase = "Numerical answer questions have no negative marking";

    private static readonly string[] Items =
    [
        "The timer starts as soon as you begin. It keeps running even if you close the page or lose your internet connection — you can reopen the test and continue with the time that is left.",
        "When the time runs out the test is submitted automatically. Everything you have answered up to that moment is saved.",
        "Marks for each section are shown in the table above. A correct answer adds marks; a wrong answer loses marks wherever negative marking applies. A question you do not answer scores zero.",
        "<strong>Numerical answer questions have no negative marking.</strong> Enter the number using the on-screen keypad, without units.",
        "For multiple choice questions, click an option to select it. Click the same option again to clear it, or click another option to change your answer.",
        "Use <strong>Mark for Review</strong> to flag a question and come back to it later. A question that is answered and marked for review is still evaluated.",
        "Use the question palette to jump straight to any question. The colours are explained above.",
        "Do not switch to another tab or application during the test. Every switch is recorded and shown on your result.",
        "Keep blank paper and a pen for rough work. Calculators, mobile phones, books and notes are not allowed unless your exam specifically permits them.",
        "You can finish early with the <strong>Submit</strong> button — you will see a summary of answered and unanswered questions before you confirm.",
        "After submitting you will see your score, a section-wise breakdown and the correct answer for every question.",
    ];

    /// <summary>The stored HTML: one line, no indentation (see the note on Quill above).</summary>
    public static readonly string Html =
        Marker +
        "<ol style=\"padding-left:1.25rem;\">" +
        string.Concat(Items.Select(item => $"<li>{item}</li>")) +
        "</ol><p><em>All the best!</em></p>";
}
