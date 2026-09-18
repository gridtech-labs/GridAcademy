namespace GridAcademy.Helpers;

/// <summary>
/// The standard instructions shown before every test, for every exam.
///
/// Single source of truth: the admin Create/Edit pages pre-fill it, TestService applies it
/// whenever a test is saved without instructions, and DbSeeder backfills tests that have
/// none. Individual tests can still override it with their own text.
///
/// Deliberately exam-agnostic (no JEE/NEET-specific marks) and written to COMPLEMENT the
/// instructions screen, which already renders the question-palette colour legend, the
/// duration, the question count, the pass mark and a per-section marks table. Repeating
/// those here would only duplicate what the student can already see.
///
/// Every statement matches how the exam engine actually behaves:
/// the timer runs server-side from the moment the attempt starts (AssessmentService),
/// numerical answers carry no negative marking whatever the section says, and tab switches
/// are recorded as violations and surfaced on the result page.
/// </summary>
public static class DefaultTestInstructions
{
    /// <summary>Marks older copies that shipped with an unreplaced "[Duration]" placeholder.</summary>
    public const string LegacyPlaceholder = "[Duration]";

    public const string Html = """
        <ol style="line-height:1.8;padding-left:1.25rem;">
          <li>The timer starts as soon as you begin. It keeps running even if you close the page
              or lose your internet connection — you can reopen the test and continue with the
              time that is left.</li>
          <li>When the time runs out the test is submitted automatically. Everything you have
              answered up to that moment is saved.</li>
          <li>Marks for each section are shown in the table above. A correct answer adds marks;
              a wrong answer loses marks wherever negative marking applies. A question you do
              not answer scores zero.</li>
          <li><strong>Numerical answer questions have no negative marking.</strong> Enter the
              number using the on-screen keypad, without units.</li>
          <li>For multiple choice questions, click an option to select it. Click the same option
              again to clear it, or click another option to change your answer.</li>
          <li>Use <strong>Mark for Review</strong> to flag a question and come back to it later.
              A question that is answered and marked for review is still evaluated.</li>
          <li>Use the question palette to jump straight to any question. The colours are
              explained above.</li>
          <li>Do not switch to another tab or application during the test. Every switch is
              recorded and shown on your result.</li>
          <li>Keep blank paper and a pen for rough work. Calculators, mobile phones, books and
              notes are not allowed unless your exam specifically permits them.</li>
          <li>You can finish early with the <strong>Submit</strong> button — you will see a
              summary of answered and unanswered questions before you confirm.</li>
          <li>After submitting you will see your score, a section-wise breakdown and the correct
              answer for every question.</li>
        </ol>
        <p><em>All the best!</em></p>
        """;
}
