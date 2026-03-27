namespace GridAcademy.Data.Entities.Content;

public class Topic : MasterBase
{
    public int     SubjectId { get; set; }
    public Subject Subject   { get; set; } = null!;

    public ICollection<Question> Questions { get; set; } = [];
}
