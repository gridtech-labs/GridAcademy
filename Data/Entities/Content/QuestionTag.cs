namespace GridAcademy.Data.Entities.Content;

public class QuestionTag
{
    public Guid QuestionId { get; set; }
    public int  TagId      { get; set; }

    public Question Question { get; set; } = null!;
    public Tag      Tag      { get; set; } = null!;
}
