namespace GridAcademy.Data.Entities.Content;

/// <summary>
/// Represents one blank in a Fill-in-the-Blanks question.
/// The question text should mark blank positions using [BLANK_1], [BLANK_2] etc.
/// </summary>
public class QuestionBlank
{
    public int    Id               { get; set; }
    public Guid   QuestionId       { get; set; }

    /// <summary>1-based position of this blank in the question text.</summary>
    public int    BlankIndex       { get; set; }

    /// <summary>Primary accepted answer (case handling governed by CaseSensitive).</summary>
    public string CorrectAnswer    { get; set; } = "";

    /// <summary>
    /// Pipe-separated list of equally-valid alternate answers.
    /// e.g. "colour|color|Colour|Color"
    /// </summary>
    public string? AlternateAnswers { get; set; }

    public bool   CaseSensitive   { get; set; } = false;

    public Question Question { get; set; } = null!;
}
