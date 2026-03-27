using GridAcademy.Data.Entities.Assessment;

namespace GridAcademy.Data.Entities.Exam;

/// <summary>Maps a published test/assessment to an exam page.</summary>
public class ExamPageTest
{
    public Guid ExamPageId  { get; set; }
    public Guid TestId      { get; set; }

    /// <summary>Allow students to take this test for free without login.</summary>
    public bool IsFree      { get; set; } = true;
    public int  SortOrder   { get; set; } = 0;

    // Navigation
    public ExamPage ExamPage { get; set; } = null!;
    public Test     Test     { get; set; } = null!;
}
