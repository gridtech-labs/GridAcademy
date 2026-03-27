namespace GridAcademy.Data.Entities.Content;

public class Subject : MasterBase
{
    public ICollection<Topic>    Topics    { get; set; } = [];
    public ICollection<Question> Questions { get; set; } = [];
}
