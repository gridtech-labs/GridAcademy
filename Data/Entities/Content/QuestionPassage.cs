namespace GridAcademy.Data.Entities.Content;

/// <summary>
/// A reading passage / comprehension text that groups one or more
/// PassageBased sub-questions.  Questions reference this via Question.PassageId.
/// </summary>
public class QuestionPassage
{
    public Guid   Id          { get; set; } = Guid.NewGuid();
    public string Title       { get; set; } = "";   // optional display title
    public string PassageText { get; set; } = "";   // full HTML/plain passage body
    public bool   IsActive    { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Question> Questions { get; set; } = [];
}
