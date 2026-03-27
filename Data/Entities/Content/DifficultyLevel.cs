namespace GridAcademy.Data.Entities.Content;

public class DifficultyLevel : MasterBase
{
    public ICollection<Question> Questions { get; set; } = [];
}
