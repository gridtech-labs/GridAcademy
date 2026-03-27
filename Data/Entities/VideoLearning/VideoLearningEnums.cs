namespace GridAcademy.Data.Entities.VideoLearning;

public enum VideoStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

public enum ProgramStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

public enum DiscountType
{
    Percentage = 0,
    FixedAmount = 1
}

public enum EnrollmentStatus
{
    Active = 0,
    Expired = 1,
    Cancelled = 2
}

public enum VideoProgressStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2
}

// LearningPathType removed — all learning paths use node-based hierarchy (VlLearningPathNode)
// ContentItemType removed — replaced by LpNodeType string constants (VL, AS, SC, PD, HT)

public enum ContentFileType
{
    Scorm = 0,
    Html  = 1,
    Pdf   = 2
}

public enum CourseLaunchStatus
{
    Active  = 0,
    Blocked = 1,
    Closed  = 2
}
