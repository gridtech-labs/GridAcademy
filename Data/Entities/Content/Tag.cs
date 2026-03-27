namespace GridAcademy.Data.Entities.Content;

public class Tag : MasterBase
{
    public ICollection<QuestionTag> QuestionTags { get; set; } = [];
}
