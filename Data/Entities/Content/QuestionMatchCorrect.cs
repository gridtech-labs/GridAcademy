namespace GridAcademy.Data.Entities.Content;

/// <summary>
/// Records a correct pairing between a left/row item and a right/column item.
///
/// MatchTheFollowing:  one row per left item  (A→p, B→r, C→s, D→q)
/// MatrixMatch:        multiple rows per row item  (I→A, I→B, II→C, ...)
/// </summary>
public class QuestionMatchCorrect
{
    public int    Id          { get; set; }
    public Guid   QuestionId  { get; set; }

    /// <summary>Label of the left/row item  e.g. "A" or "I"</summary>
    public string LeftLabel   { get; set; } = "";

    /// <summary>Label of the right/column item  e.g. "p" or "C"</summary>
    public string RightLabel  { get; set; } = "";

    public Question Question  { get; set; } = null!;
}
