namespace GridAcademy.Data.Entities.Content;

public class ExamType : MasterBase
{
    public ICollection<Question> Questions { get; set; } = [];
}
