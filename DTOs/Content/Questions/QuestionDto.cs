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

public class QuestionDto
{
    public Guid   Id               { get; set; }
    public string Text             { get; set; } = "";
    public string? Solution        { get; set; }
    public QuestionType   QuestionType    { get; set; }
    public QuestionStatus Status          { get; set; }
    public string?        Subtopic        { get; set; }

    // Master references
    public int    SubjectId         { get; set; }
    public string SubjectName       { get; set; } = "";
    public int    TopicId           { get; set; }
    public string TopicName         { get; set; } = "";
    public int    DifficultyLevelId { get; set; }
    public string DifficultyLevel   { get; set; } = "";
    public int    ComplexityLevelId { get; set; }
    public string ComplexityLevel   { get; set; } = "";
    public int    MarksId           { get; set; }
    public decimal Marks            { get; set; }
    public int    NegativeMarksId   { get; set; }
    public decimal NegativeMarks    { get; set; }
    public int    ExamTypeId        { get; set; }
    public string ExamType          { get; set; } = "";

    public decimal? NumericalAnswer { get; set; }

    public List<QuestionOptionDto> Options { get; set; } = [];
    public List<TagDto>            Tags    { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
