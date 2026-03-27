namespace GridAcademy.Data.Entities.Content;

public class MarksMaster : MasterBase
{
    public decimal Value { get; set; }   // e.g. 1, 2, 4

    public ICollection<Question> Questions { get; set; } = [];
}
