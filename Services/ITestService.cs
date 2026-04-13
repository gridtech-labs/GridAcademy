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

    // ── Manual question management ────────────────────────────────────────

    /// <summary>Returns all questions directly mapped to a test, ordered by SortOrder.</summary>
    Task<List<TestQuestionDto>> GetTestQuestionsAsync(Guid testId);

    /// <summary>
    /// Returns up to 50 Published questions from the bank, filtered by optional
    /// subjectId, difficultyLevelId, and a text search. Each item flags whether
    /// the question is already mapped to the given test.
    /// </summary>
    Task<List<QuestionBrowseItem>> BrowseQuestionsForTestAsync(
        Guid testId, int? subjectId, int? difficultyLevelId, string? search);

    /// <summary>Directly maps a list of question IDs to a test (duplicates are ignored).</summary>
    Task AddQuestionsToTestAsync(Guid testId, List<Guid> questionIds);

    /// <summary>Removes a single question from the test's direct mapping.</summary>
    Task RemoveQuestionFromTestAsync(Guid testId, Guid questionId);

    // ── Pool validation ───────────────────────────────────────────────────

    /// <summary>
    /// Returns the count of Published questions that satisfy the given section's
    /// SubjectId constraint and (if set) DifficultyLevelId constraint.
    /// </summary>
    Task<int> GetSectionPoolCountAsync(int sectionId);
}
