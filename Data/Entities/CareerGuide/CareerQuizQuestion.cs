namespace GridAcademy.Data.Entities.CareerGuide;

public class CareerQuizQuestion
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<CareerQuizOption> Options { get; set; } = [];
}
