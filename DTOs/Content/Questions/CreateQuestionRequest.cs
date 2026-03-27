using System.ComponentModel.DataAnnotations;
using GridAcademy.Data.Entities.Content;

namespace GridAcademy.DTOs.Content.Questions;

public class CreateOptionRequest
{
    [Required] public char   Label     { get; set; }
    [Required] public string Text      { get; set; } = "";
               public bool   IsCorrect { get; set; }
}

public class CreateQuestionRequest
{
    [Required] public string Text      { get; set; } = "";
               public string? Solution { get; set; }
               public string? Subtopic { get; set; }

    [Required] public QuestionType QuestionType { get; set; }

    [Range(1, int.MaxValue)] public int SubjectId         { get; set; }
    [Range(1, int.MaxValue)] public int TopicId           { get; set; }
    [Range(1, int.MaxValue)] public int DifficultyLevelId { get; set; }
    [Range(1, int.MaxValue)] public int ComplexityLevelId { get; set; }
    [Range(1, int.MaxValue)] public int MarksId           { get; set; }
    [Range(1, int.MaxValue)] public int NegativeMarksId   { get; set; }
    [Range(1, int.MaxValue)] public int ExamTypeId        { get; set; }

    public decimal? NumericalAnswer { get; set; }

    public List<CreateOptionRequest> Options { get; set; } = [];
    public List<int>                 TagIds  { get; set; } = [];
}

public class UpdateQuestionRequest : CreateQuestionRequest
{
    public QuestionStatus Status { get; set; } = QuestionStatus.Draft;
}
