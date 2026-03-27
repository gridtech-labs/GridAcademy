namespace GridAcademy.Data.Entities.Content;

/// <summary>
/// One item in the left or right column of a MatchTheFollowing question,
/// OR one row/column header in a MatrixMatch question.
///
/// MatchTheFollowing layout:
///   ColumnSide = "left"  → List I  items (A, B, C, D)
///   ColumnSide = "right" → List II items (p, q, r, s)
///
/// MatrixMatch layout:
///   ColumnSide = "row"   → Row    items (I, II, III, IV)
///   ColumnSide = "col"   → Column items (A, B, C, D)
/// </summary>
public class QuestionMatchItem
{
    public int    Id         { get; set; }
    public Guid   QuestionId { get; set; }

    /// <summary>"left" | "right" (MTF)  or  "row" | "col" (MMQ)</summary>
    public string ColumnSide { get; set; } = "";

    /// <summary>Display label: 'A','B','C','D' / 'p','q','r','s' / 'I','II','III','IV'</summary>
    public string Label      { get; set; } = "";

    public string Text       { get; set; } = "";
    public int    SortOrder  { get; set; } = 0;

    public Question Question { get; set; } = null!;
}
