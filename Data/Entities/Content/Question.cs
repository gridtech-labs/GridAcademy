namespace GridAcademy.Data.Entities.Content;

public class Question
{
    public Guid   Id        { get; set; } = Guid.NewGuid();

    /// <summary>Main question stem / body (plain text or HTML).</summary>
    public string  Text    { get; set; } = "";

    /// <summary>Optional worked solution shown after submission.</summary>
    public string? Solution { get; set; }

    /// <summary>Free-text sub-topic / chapter label within the topic.</summary>
    public string? Subtopic { get; set; }

    // ── Classification ─────────────────────────────────────────────────────
    public QuestionType   QuestionType { get; set; }
    public QuestionStatus Status       { get; set; } = QuestionStatus.Draft;

    // ── FK references ──────────────────────────────────────────────────────
    public int SubjectId          { get; set; }
    public int TopicId            { get; set; }
    public int DifficultyLevelId  { get; set; }
    public int ComplexityLevelId  { get; set; }
    public int MarksId            { get; set; }
    public int NegativeMarksId    { get; set; }
    public int ExamTypeId         { get; set; }

    // ── Type-specific columns ──────────────────────────────────────────────

    /// <summary>[NAT] Exact numerical answer.</summary>
    public decimal? NumericalAnswer    { get; set; }

    /// <summary>[NAT] Acceptable deviation ± around NumericalAnswer (0 = exact match).</summary>
    public decimal? NumericalTolerance { get; set; }

    /// <summary>[AssertionReason] The Assertion statement text (A).</summary>
    public string? AssertionText { get; set; }

    /// <summary>[AssertionReason] The Reason statement text (R).</summary>
    public string? ReasonText    { get; set; }

    /// <summary>[PassageBased] FK → question_passages — groups sub-questions under a passage.</summary>
    public Guid?   PassageId    { get; set; }

    // ── Navigations ────────────────────────────────────────────────────────
    public QuestionTypeMaster  QuestionTypeMaster { get; set; } = null!;
    public Subject             Subject            { get; set; } = null!;
    public Topic               Topic              { get; set; } = null!;
    public DifficultyLevel     DifficultyLevel    { get; set; } = null!;
    public ComplexityLevel     ComplexityLevel    { get; set; } = null!;
    public MarksMaster         Marks              { get; set; } = null!;
    public NegativeMarksMaster NegativeMarks      { get; set; } = null!;
    public ExamType            ExamType           { get; set; } = null!;
    public QuestionPassage?    Passage            { get; set; }

    // ── Child collections ──────────────────────────────────────────────────

    /// <summary>[MCQ / MSQ / TrueFalse / AssertionReason] Answer options.</summary>
    public ICollection<QuestionOption>       Options      { get; set; } = [];

    public ICollection<QuestionTag>          QuestionTags { get; set; } = [];

    /// <summary>[FillInBlanks] One entry per [BLANK_n] in the question text.</summary>
    public ICollection<QuestionBlank>        Blanks       { get; set; } = [];

    /// <summary>[MatchTheFollowing / MatrixMatch] Left and right column items.</summary>
    public ICollection<QuestionMatchItem>    MatchItems   { get; set; } = [];

    /// <summary>[MatchTheFollowing / MatrixMatch] Correct pair / cell links.</summary>
    public ICollection<QuestionMatchCorrect> MatchCorrect { get; set; } = [];

    // ── Audit ──────────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid?    CreatedBy { get; set; }
    public Guid?    UpdatedBy { get; set; }
}
