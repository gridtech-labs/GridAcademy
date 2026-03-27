namespace GridAcademy.Data.Entities.Content;

public class NegativeMarksMaster : MasterBase
{
    public decimal Value { get; set; }   // e.g. 0, -0.25, -1

    public ICollection<Question> Questions { get; set; } = [];
}
