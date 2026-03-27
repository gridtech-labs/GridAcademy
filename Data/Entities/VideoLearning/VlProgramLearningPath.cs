namespace GridAcademy.Data.Entities.VideoLearning;

public class VlProgramLearningPath
{
    public Guid LearningPathId { get; set; }
    public Guid ProgramId      { get; set; }
    public int  SortOrder      { get; set; } = 0;

    public VlLearningPath LearningPath { get; set; } = null!;
    public VlProgram      Program      { get; set; } = null!;
}
