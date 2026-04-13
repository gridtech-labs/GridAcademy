using GridAcademy.Data.Entities.Assessment;

namespace GridAcademy.DTOs.Assessment;

// ── Test list / admin views ───────────────────────────────────────────────

public class TestListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string ExamTypeName { get; set; } = "";
    public TestStatus Status { get; set; }
    public int SectionCount { get; set; }
    public int TotalQuestions { get; set; }  // sum of QuestionCount across sections
    public int DurationMinutes { get; set; }
    public decimal PassingPercent { get; set; }
    public bool NegativeMarkingEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TestDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? Instructions { get; set; }
    public int DurationMinutes { get; set; }
    public decimal PassingPercent { get; set; }
    public bool NegativeMarkingEnabled { get; set; }
    public int ExamTypeId { get; set; }
    public string ExamTypeName { get; set; } = "";
    public TestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<TestSectionDto> Sections { get; set; } = [];
}

public class TestSectionDto
{
    public int Id { get; set; }
    public Guid TestId { get; set; }
    public string Name { get; set; } = "";
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    public int? DifficultyLevelId { get; set; }
    public string? DifficultyLevelName { get; set; }
    public int QuestionCount { get; set; }
    public decimal MarksPerQuestion { get; set; }
    public decimal NegativeMarksPerQuestion { get; set; }
    public int SortOrder { get; set; }
    public int AvailableInPool { get; set; }  // filled by ValidateSectionPool
}

// ── Requests ─────────────────────────────────────────────────────────────

public class CreateTestRequest
{
    public string Title { get; set; } = "";
    public string? Instructions { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public decimal PassingPercent { get; set; } = 35;
    public bool NegativeMarkingEnabled { get; set; } = false;
    public int ExamTypeId { get; set; }
}

public class UpdateTestRequest : CreateTestRequest { }

public class CreateTestSectionRequest
{
    public string Name { get; set; } = "";
    public int SubjectId { get; set; }
    public int? DifficultyLevelId { get; set; }
    public int QuestionCount { get; set; } = 10;
    public decimal MarksPerQuestion { get; set; } = 4;
    public decimal NegativeMarksPerQuestion { get; set; } = 1;
    public int SortOrder { get; set; } = 0;
}

public class TestListRequest
{
    public string? Search { get; set; }
    public TestStatus? Status { get; set; }
    public int? ExamTypeId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AssignTestRequest
{
    public List<Guid> StudentIds { get; set; } = [];
    public DateTime AvailableFrom { get; set; }
    public DateTime AvailableTo { get; set; }
    public int MaxAttempts { get; set; } = 1;
}

public class TestAssignmentDto
{
    public Guid Id { get; set; }
    public Guid TestId { get; set; }
    public string TestTitle { get; set; } = "";
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public string StudentEmail { get; set; } = "";
    public DateTime AvailableFrom { get; set; }
    public DateTime AvailableTo { get; set; }
    public int MaxAttempts { get; set; }
    public int AttemptsUsed { get; set; }
    public bool IsActive { get; set; }  // AvailableFrom <= now <= AvailableTo
}

// ── Manual question mapping ───────────────────────────────────────────────

/// <summary>A question directly mapped to a test (via TestQuestion table).</summary>
public class TestQuestionDto
{
    public Guid   QuestionId      { get; set; }
    public string Text            { get; set; } = "";
    public string SubjectName     { get; set; } = "";
    public string DifficultyLevel { get; set; } = "";
    public string QuestionType    { get; set; } = "";
    public int    SortOrder       { get; set; }
}

/// <summary>A question from the bank shown in the "Add Questions" picker modal.</summary>
public class QuestionBrowseItem
{
    public Guid   Id              { get; set; }
    public string Text            { get; set; } = "";
    public string SubjectName     { get; set; } = "";
    public string TopicName       { get; set; } = "";
    public string DifficultyLevel { get; set; } = "";
    public string QuestionType    { get; set; } = "";
    public bool   AlreadyMapped   { get; set; }
}

// ── Student-facing ────────────────────────────────────────────────────────

public class StudentTestCardDto
{
    public Guid AssignmentId { get; set; }
    public Guid TestId { get; set; }
    public string Title { get; set; } = "";
    public string ExamTypeName { get; set; } = "";
    public int DurationMinutes { get; set; }
    public int TotalQuestions { get; set; }
    public int SectionCount { get; set; }
    public decimal PassingPercent { get; set; }
    public bool NegativeMarkingEnabled { get; set; }
    public DateTime AvailableFrom { get; set; }
    public DateTime AvailableTo { get; set; }
    public int MaxAttempts { get; set; }
    public int AttemptsUsed { get; set; }
    public int AttemptsRemaining { get; set; }
    public bool HasInProgressAttempt { get; set; }
    public Guid? InProgressAttemptId { get; set; }
    public Guid? LastCompletedAttemptId { get; set; }
    public List<TestSectionDto> Sections { get; set; } = [];
}
