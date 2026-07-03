using GridAcademy.Data.Entities.Assessment;

namespace GridAcademy.DTOs.Assessment;

// ── Student Activity Report (admin) ───────────────────────────────────────

/// <summary>
/// One row per student in the activity report list — aggregated stats across
/// all of that student's test attempts.
/// </summary>
public class StudentActivitySummaryDto
{
    public Guid     StudentId          { get; set; }
    public string   StudentName        { get; set; } = "";
    public string   Email              { get; set; } = "";
    public string?  Phone              { get; set; }
    public int      TotalAttempts      { get; set; }
    public int      CompletedAttempts  { get; set; }
    public int      PassedCount        { get; set; }
    public decimal? AveragePercentage  { get; set; }
    public decimal? BestPercentage     { get; set; }
    public DateTime LastActivityAt     { get; set; }
}

/// <summary>One attempt row inside a single student's activity detail.</summary>
public class StudentAttemptRowDto
{
    public Guid          AttemptId          { get; set; }
    public string        TestTitle          { get; set; } = "";
    public int           AttemptNumber      { get; set; }
    public AttemptStatus Status             { get; set; }
    public DateTime      StartedAt          { get; set; }
    public DateTime?     SubmittedAt        { get; set; }
    public int           DurationSecondsUsed{ get; set; }
    public decimal?      TotalMarksObtained { get; set; }
    public decimal?      TotalMarksPossible { get; set; }
    public decimal?      Percentage         { get; set; }
    public bool?         IsPassed           { get; set; }
    public int           ViolationCount     { get; set; }
}

/// <summary>Full activity detail for a single student: profile + all attempts.</summary>
public class StudentActivityDetailDto
{
    public Guid     StudentId          { get; set; }
    public string   StudentName        { get; set; } = "";
    public string   Email              { get; set; } = "";
    public string?  Phone              { get; set; }
    public string   Role               { get; set; } = "";
    public bool     IsActive           { get; set; }
    public DateTime JoinedAt           { get; set; }
    public DateTime? LastLoginAt       { get; set; }

    // Aggregates
    public int      TotalAttempts      { get; set; }
    public int      CompletedAttempts  { get; set; }
    public int      PassedCount        { get; set; }
    public int      FailedCount        { get; set; }
    public decimal? AveragePercentage  { get; set; }
    public decimal? BestPercentage     { get; set; }

    public List<StudentAttemptRowDto> Attempts { get; set; } = [];
}
