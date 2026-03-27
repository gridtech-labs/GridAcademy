namespace GridAcademy.Data.Entities.Content;

/// <summary>
/// One answer option for MCQ / MSQ / TrueFalse / AssertionReason questions.
/// Label is a single character: 'A','B','C','D' for MCQ; 'T','F' for True/False.
/// </summary>
public class QuestionOption
{
    public int    Id         { get; set; }
    public Guid   QuestionId { get; set; }

    /// <summary>'A'–'D' for MCQ/MSQ, 'T'/'F' for TrueFalse, '1'–'4' for AssertionReason options.</summary>
    public char   Label      { get; set; }

    public string Text       { get; set; } = "";
    public bool   IsCorrect  { get; set; }
    public int    SortOrder  { get; set; } = 0;

    public Question Question { get; set; } = null!;
}
