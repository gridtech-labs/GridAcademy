namespace GridAcademy.Data.Entities.Content;

public class ComplexityLevel : MasterBase
{
    public ICollection<Question> Questions { get; set; } = [];
}
