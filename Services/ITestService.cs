using GridAcademy.DTOs.Assessment;

namespace GridAcademy.Services;

public interface ITestService
{
    // ── Test CRUD ─────────────────────────────────────────────────────────
    Task<List<TestListDto>>  GetTestsAsync(TestListRequest request);
    Task<TestDetailDto>      GetTestByIdAsync(Guid id);
    Task<TestDetailDto>      CreateTestAsync(CreateTestRequest request, Guid createdBy);
    Task<TestDetailDto>      UpdateTestAsync(Guid id, UpdateTestRequest request, Guid updatedBy);

    /// <summary>
    /// Transitions a test from Draft → Published.
    /// Throws <see cref="InvalidOperationException"/> if the test has no sections
    /// or if any section's question pool is too small to satisfy its QuestionCount.
    /// </summary>
    Task PublishTestAsync(Guid id);

    /// <summary>
    /// Transitions a test from Published → Draft.
    /// Throws <see cref="InvalidOperationException"/> if active (InProgress) attempts exist.
    /// </summary>
    Task UnpublishTestAsync(Guid id);

    /// <summary>
    /// Permanently deletes a test.
    /// Throws <see cref="InvalidOperationException"/> if the test has any assignments.
    /// </summary>
    Task DeleteTestAsync(Guid id);

    // ── Sections ──────────────────────────────────────────────────────────
    Task<TestSectionDto> AddSectionAsync(Guid testId, CreateTestSectionRequest request);
    Task<TestSectionDto> UpdateSectionAsync(Guid testId, int sectionId, CreateTestSectionRequest request);
    Task                 DeleteSectionAsync(Guid testId, int sectionId);

    // ── Assignments ───────────────────────────────────────────────────────
    Task<List<TestAssignmentDto>> AssignTestAsync(Guid testId, AssignTestRequest request, Guid assignedBy);
    Task<List<TestAssignmentDto>> GetAssignmentsAsync(Guid testId);
    Task<List<TestAssignmentDto>> GetStudentAssignmentsAsync(Guid studentId);
    Task                          RevokeAssignmentAsync(Guid assignmentId);

    // ── Pool validation ───────────────────────────────────────────────────

    /// <summary>
    /// Returns the count of Published questions that satisfy the given section's
    /// SubjectId constraint and (if set) DifficultyLevelId constraint.
    /// </summary>
    Task<int> GetSectionPoolCountAsync(int sectionId);
}
