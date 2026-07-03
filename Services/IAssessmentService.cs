using GridAcademy.Common;
using GridAcademy.DTOs.Assessment;

namespace GridAcademy.Services;

public interface IAssessmentService
{
    // ── Student-facing ────────────────────────────────────────────────────

    /// <summary>Returns all test cards for a student, enriched with attempt progress.</summary>
    Task<List<StudentTestCardDto>> GetAvailableTestsAsync(Guid studentId);

    /// <summary>
    /// Starts a new attempt for the given assignment.
    /// Randomly selects questions per section and persists AttemptQuestion rows.
    /// Throws <see cref="InvalidOperationException"/> if an in-progress attempt already exists
    /// or if the assignment window / attempt limit has been exceeded.
    /// </summary>
    Task<AttemptStartDto> StartAttemptAsync(Guid assignmentId, Guid studentId);

    /// <summary>
    /// Returns lightweight test info (title, duration, sections, instructions) for the
    /// instructions page. Does NOT load question data. Validates student ownership.
    /// </summary>
    Task<AttemptInfoDto> GetAttemptInfoAsync(Guid attemptId, Guid studentId);

    /// <summary>
    /// Marks the attempt's InstructionsAcknowledged flag as true.
    /// Called when the student clicks "Start Test" on the instructions page.
    /// </summary>
    Task AcknowledgeInstructionsAsync(Guid attemptId, Guid studentId);

    /// <summary>
    /// Returns the full attempt state (questions + saved answers) for a resume scenario.
    /// Validates that the attempt belongs to the requesting student.
    /// </summary>
    Task<AttemptStateDto> GetAttemptStateAsync(Guid attemptId, Guid studentId);

    /// <summary>
    /// Upserts the student's answer for one question within an in-progress attempt.
    /// If the time limit has been exceeded, auto-submits the attempt and throws
    /// <see cref="InvalidOperationException"/> with message "Time expired".
    /// </summary>
    Task SaveAnswerAsync(Guid attemptId, SaveAnswerRequest request, Guid studentId);

    /// <summary>Toggles the IsMarkedForReview flag on the given AttemptQuestion.</summary>
    Task ToggleMarkForReviewAsync(Guid attemptId, Guid questionId, Guid studentId);

    /// <summary>Sets IsVisited = true on the given AttemptQuestion.</summary>
    Task MarkVisitedAsync(Guid attemptId, Guid questionId, Guid studentId);

    /// <summary>
    /// Appends a proctoring violation event to the attempt's ViolationLog JSON array.
    /// </summary>
    Task LogViolationAsync(Guid attemptId, string violationType, Guid studentId);

    /// <summary>
    /// Scores and finalises the attempt.
    /// Writes AttemptSectionResult rows, sets TotalMarksObtained, Percentage, IsPassed.
    /// Returns the full result DTO.
    /// </summary>
    Task<AttemptResultDto> SubmitAttemptAsync(Guid attemptId, Guid studentId);

    /// <summary>Returns the result for a submitted/timed-out attempt belonging to the student.</summary>
    Task<AttemptResultDto> GetResultAsync(Guid attemptId, Guid studentId);

    /// <summary>
    /// Returns performance summary (stats + full attempt list) for a student.
    /// Only includes Submitted and TimedOut attempts, ordered newest-first.
    /// </summary>
    Task<MyPerformanceDto> GetMyPerformanceAsync(Guid studentId);

    // ── Admin ─────────────────────────────────────────────────────────────

    /// <summary>Returns a summary list of all attempts for a test (admin view).</summary>
    Task<List<AttemptSummaryDto>> GetAttemptsByTestAsync(Guid testId);

    /// <summary>
    /// Returns the full attempt detail including correct answers (admin view).
    /// Does not validate student ownership.
    /// </summary>
    Task<AttemptResultDto> GetAttemptDetailAsync(Guid attemptId);

    // ── Student Activity Report (admin) ───────────────────────────────────

    /// <summary>
    /// Returns a paged, searchable list of students who have attempted at least
    /// one test, with aggregated stats. Search matches name, email or phone.
    /// </summary>
    Task<PagedResult<StudentActivitySummaryDto>> GetStudentActivityAsync(
        string? search, int page, int pageSize);

    /// <summary>
    /// Returns the full activity detail (profile + every attempt) for one student.
    /// Returns null when the student does not exist.
    /// </summary>
    Task<StudentActivityDetailDto?> GetStudentActivityDetailAsync(Guid studentId);
}
