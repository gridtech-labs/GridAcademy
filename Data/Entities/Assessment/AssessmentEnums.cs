namespace GridAcademy.Data.Entities.Assessment;

public enum TestStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3
}

public enum AttemptStatus
{
    InProgress = 1,
    Submitted = 2,
    TimedOut = 3,
    Abandoned = 4
}

/// <summary>
/// Controls how questions are selected when a student takes the test.
/// GlobalBank = questions are randomly picked from the pool matching section criteria.
/// Manual     = specific questions explicitly added and assigned to sections by the teacher.
/// </summary>
public enum QuestionMode
{
    GlobalBank = 0,
    Manual     = 1
}
