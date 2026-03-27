using GridAcademy.Data.Entities.Content;

namespace GridAcademy.DTOs.Content.Questions;

public class QuestionListRequest
{
    public string?        Search          { get; set; }
    public int?           SubjectId       { get; set; }
    public int?           TopicId         { get; set; }
    public int?           DifficultyLevelId { get; set; }
    public int?           ExamTypeId      { get; set; }
    public QuestionType?  QuestionType    { get; set; }
    public QuestionStatus? Status         { get; set; }
    public int            Page            { get; set; } = 1;
    public int            PageSize        { get; set; } = 20;
}
