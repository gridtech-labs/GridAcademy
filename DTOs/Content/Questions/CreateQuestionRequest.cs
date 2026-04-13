using System.ComponentModel.DataAnnotations;
using GridAcademy.Data.Entities.Content;

namespace GridAcademy.DTOs.Content.Questions;

// ── Option (MCQ / MSQ / TrueFalse / AssertionReason) ─────────────────────────
public class CreateOptionRequest
{
    [Required] public char   Label     { get; set; }
    [Required] public string Text      { get; set; } = "";
               public bool   IsCorrect { get; set; }
}

// ── Fill-in-the-Blank ─────────────────────────────────────────────────────────
public class CreateBlankRequest
{
    public int     BlankIndex       { get; set; } = 1;
    public string  CorrectAnswer    { get; set; } = "";
    /// <summary>Pipe-separated alternate accepted answers, e.g. "colour|Color".</summary>
    public string? AlternateAnswers { get; set; }
    public bool    CaseSensitive    { get; set; }
}

// ── Match item (MatchTheFollowing / MatrixMatch) ───────────────────────────────
public class CreateMatchItemRequest
{
    /// <summary>"left" | "right" for MTF; "row" | "col" for MMQ.</summary>
    public string ColumnSide { get; set; } = "";
    public string Label      { get; set; } = "";
    public string Text       { get; set; } = "";
    public int    SortOrder  { get; set; }
}

// ── Correct pairing (MatchTheFollowing / MatrixMatch) ─────────────────────────
public class CreateMatchCorrectRequest
{
    public string LeftLabel  { get; set; } = "";
    public string RightLabel { get; set; } = "";
}

// ── Passage sub-question ──────────────────────────────────────────────────────
public class CreateSubQuestionRequest
{
    public string       Text         { get; set; } = "";
    public string?      Solution     { get; set; }
    public QuestionType QuestionType { get; set; } = QuestionType.MCQ;
    public List<CreateOptionRequest> Options { get; set; } = [];
}

// ── Main create request ───────────────────────────────────────────────────────
public class CreateQuestionRequest
{
    [Required] public string  Text      { get; set; } = "";
               public string? Solution  { get; set; }
               public string? Subtopic  { get; set; }

    [Required] public QuestionType QuestionType { get; set; }

    [Range(1, int.MaxValue)] public int SubjectId         { get; set; }
    [Range(1, int.MaxValue)] public int TopicId           { get; set; }
    [Range(1, int.MaxValue)] public int DifficultyLevelId { get; set; }
    [Range(1, int.MaxValue)] public int ComplexityLevelId { get; set; }
    [Range(1, int.MaxValue)] public int MarksId           { get; set; }
    [Range(1, int.MaxValue)] public int NegativeMarksId   { get; set; }
    [Range(1, int.MaxValue)] public int ExamTypeId        { get; set; }

    // ── NAT ──────────────────────────────────────────────────────────────────
    public decimal? NumericalAnswer    { get; set; }
    public decimal? NumericalTolerance { get; set; }

    // ── MCQ / MSQ / TrueFalse / AssertionReason ───────────────────────────────
    public List<CreateOptionRequest> Options { get; set; } = [];

    // ── FillInBlanks ──────────────────────────────────────────────────────────
    public List<CreateBlankRequest> Blanks { get; set; } = [];

    // ── MatchTheFollowing / MatrixMatch ───────────────────────────────────────
    public List<CreateMatchItemRequest>    MatchItems   { get; set; } = [];
    public List<CreateMatchCorrectRequest> MatchCorrect { get; set; } = [];

    // ── AssertionReason ───────────────────────────────────────────────────────
    public string? AssertionText { get; set; }
    public string? ReasonText    { get; set; }

    // ── PassageBased ──────────────────────────────────────────────────────────
    public string? PassageTitle { get; set; }
    public string? PassageText  { get; set; }
    /// <summary>Set in edit mode to update an existing passage rather than create a new one.</summary>
    public Guid?   PassageId    { get; set; }
    public List<CreateSubQuestionRequest> SubQuestions { get; set; } = [];

    // ── Tags ──────────────────────────────────────────────────────────────────
    public List<int> TagIds { get; set; } = [];

    // ── Status (default Draft; set to Published when creating from a test) ────
    public QuestionStatus Status { get; set; } = QuestionStatus.Published;
}

public class UpdateQuestionRequest : CreateQuestionRequest { }
