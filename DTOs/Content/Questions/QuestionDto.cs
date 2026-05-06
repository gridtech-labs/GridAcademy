using GridAcademy.Data.Entities.Content;
using GridAcademy.DTOs.Content.Masters;

namespace GridAcademy.DTOs.Content.Questions;

public class QuestionOptionDto
{
    public int    Id        { get; set; }
    public char   Label     { get; set; }
    public string Text      { get; set; } = "";
    public bool   IsCorrect { get; set; }
}

public class QuestionBlankDto
{
    public int     BlankIndex       { get; set; }
    public string  CorrectAnswer    { get; set; } = "";
    public string? AlternateAnswers { get; set; }
    public bool    CaseSensitive    { get; set; }
}

public class QuestionMatchItemDto
{
    public string ColumnSide { get; set; } = "";
    public string Label      { get; set; } = "";
    public string Text       { get; set; } = "";
    public int    SortOrder  { get; set; }
}

public class QuestionMatchCorrectDto
{
    public string LeftLabel  { get; set; } = "";
    public string RightLabel { get; set; } = "";
}

public class QuestionPassageDto
{
    public Guid   Id          { get; set; }
    public string Title       { get; set; } = "";
    public string PassageText { get; set; } = "";
}

public class QuestionDto
{
    public Guid           Id           { get; set; }
    public string         Text         { get; set; } = "";
    public string?        Solution     { get; set; }
    public QuestionType   QuestionType { get; set; }
    public QuestionStatus Status       { get; set; }
    public string?        Subtopic     { get; set; }

    // ── Master references ─────────────────────────────────────────────────────
    public int     SubjectId         { get; set; }
    public string  SubjectName       { get; set; } = "";
    public int     TopicId           { get; set; }
    public string  TopicName         { get; set; } = "";
    public int     DifficultyLevelId { get; set; }
    public string  DifficultyLevel   { get; set; } = "";
    public int     ComplexityLevelId { get; set; }
    public string  ComplexityLevel   { get; set; } = "";
    public int     MarksId           { get; set; }
    public decimal Marks             { get; set; }
    public int     NegativeMarksId   { get; set; }
    public decimal NegativeMarks     { get; set; }

    // ── Type-specific fields ──────────────────────────────────────────────────
    public decimal? NumericalAnswer    { get; set; }
    public decimal? NumericalTolerance { get; set; }
    public string?  AssertionText      { get; set; }
    public string?  ReasonText         { get; set; }

    // ── Collections ───────────────────────────────────────────────────────────
    public List<QuestionOptionDto>       Options      { get; set; } = [];
    public List<QuestionBlankDto>        Blanks       { get; set; } = [];
    public List<QuestionMatchItemDto>    MatchItems   { get; set; } = [];
    public List<QuestionMatchCorrectDto> MatchCorrect { get; set; } = [];
    public QuestionPassageDto?           Passage      { get; set; }
    public List<TagDto>                  Tags         { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
