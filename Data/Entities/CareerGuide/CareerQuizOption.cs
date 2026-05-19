namespace GridAcademy.Data.Entities.CareerGuide;

public class CareerQuizOption
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string OptionText { get; set; } = "";
    /// <summary>One of: makers | connectors | explorers | screen-workers | thinkers | performers | healers | builders</summary>
    public string CareerCategory { get; set; } = "";
    public int SortOrder { get; set; }
    public CareerQuizQuestion Question { get; set; } = null!;
}
