namespace GridAcademy.Data.Entities.Content;

/// <summary>
/// Master table that names and describes each QuestionType enum value.
/// IDs (1-9) are seeded to match the enum; they are NOT auto-generated.
/// Admins can rename / deactivate entries but cannot add new rows without
/// a corresponding code change to the QuestionType enum.
/// </summary>
public class QuestionTypeMaster
{
    public QuestionType Id   { get; set; }        // PK = enum value; stored as int in DB
    public string Name        { get; set; } = ""; // e.g. "MCQ – Single Correct"
    public string Code        { get; set; } = ""; // short code e.g. "MCQ", "NAT", "FIB"
    public string? Description { get; set; }      // shown as tooltip / help text
    public bool   IsActive    { get; set; } = true;
    public int    SortOrder   { get; set; } = 0;

    public ICollection<Question> Questions { get; set; } = [];
}
