using GridAcademy.Common;
using GridAcademy.Data;
using GridAcademy.Data.Entities.Assessment;
using GridAcademy.Data.Entities.Exam;
using GridAcademy.DTOs.Assessment;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GridAcademy.Controllers;

[ApiController]
[Route("api/assessment")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AssessmentController(IAssessmentService assessmentSvc, AppDbContext db) : ControllerBase
{
    private Guid UserId => Guid.Parse(
        User.FindFirst("sub")?.Value ??
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
        Guid.Empty.ToString());

    // ── FREE TEST ACCESS ──────────────────────────────────────────────────
    /// <summary>
    /// Grants a registered user access to a test marked as free on a published exam.
    /// Creates (or reuses) a TestAssignment and returns assignmentId + any in-progress attemptId.
    /// </summary>
    [HttpPost("free-access/{testId:guid}")]
    public async Task<IActionResult> GetFreeAccess(Guid testId)
    {
        var isFree = await db.ExamPageTests
            .Include(t => t.ExamPage)
            .AnyAsync(t => t.TestId == testId
                && t.IsFree
                && t.ExamPage.Status == ExamPageStatus.Published
                && t.ExamPage.IsActive);

        if (!isFree)
            return NotFound(ApiResponse.Fail("This test is not available for free access."));

        var sid = UserId;

        var assignment = await db.TestAssignments
            .Include(a => a.Attempts)
            .FirstOrDefaultAsync(a => a.TestId == testId && a.StudentId == sid);

        if (assignment == null)
        {
            assignment = new TestAssignment
            {
                TestId        = testId,
                StudentId     = sid,
                AvailableFrom = DateTime.UtcNow,
                AvailableTo   = DateTime.UtcNow.AddYears(1),
                MaxAttempts   = 99,
                AssignedAt    = DateTime.UtcNow,
            };
            db.TestAssignments.Add(assignment);
            await db.SaveChangesAsync();
        }

        var inProgress = assignment.Attempts
            .OrderByDescending(a => a.StartedAt)
            .FirstOrDefault(a => a.Status == AttemptStatus.InProgress);

        return Ok(ApiResponse<object>.Ok(new {
            assignmentId = assignment.Id,
            attemptId    = inProgress?.Id as Guid?
        }));
    }

    // ── ATTEMPT LIFECYCLE ─────────────────────────────────────────────────

    [HttpPost("attempts/{assignmentId:guid}/start")]
    public async Task<IActionResult> Start(Guid assignmentId)
    {
        try
        {
            var result = await assessmentSvc.StartAttemptAsync(assignmentId, UserId);
            return Ok(ApiResponse<object>.Ok(result));
        }
        catch (Exception ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpGet("attempts/{attemptId:guid}/state")]
    public async Task<IActionResult> State(Guid attemptId)
    {
        try
        {
            var result = await assessmentSvc.GetAttemptStateAsync(attemptId, UserId);
            return Ok(ApiResponse<object>.Ok(result));
        }
        catch (Exception ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPost("attempts/{attemptId:guid}/answer")]
    public async Task<IActionResult> SaveAnswer(Guid attemptId, [FromBody] SaveAnswerRequest request)
    {
        try
        {
            await assessmentSvc.SaveAnswerAsync(attemptId, request, UserId);
            return Ok(ApiResponse<object>.Ok(null));
        }
        catch (Exception ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPost("attempts/{attemptId:guid}/mark-review/{questionId:guid}")]
    public async Task<IActionResult> ToggleMarkForReview(Guid attemptId, Guid questionId)
    {
        try
        {
            await assessmentSvc.ToggleMarkForReviewAsync(attemptId, questionId, UserId);
            return Ok(ApiResponse<object>.Ok(null));
        }
        catch (Exception ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPost("attempts/{attemptId:guid}/visited/{questionId:guid}")]
    public async Task<IActionResult> MarkVisited(Guid attemptId, Guid questionId)
    {
        try
        {
            await assessmentSvc.MarkVisitedAsync(attemptId, questionId, UserId);
            return Ok(ApiResponse<object>.Ok(null));
        }
        catch (Exception ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPost("attempts/{attemptId:guid}/violation")]
    public async Task<IActionResult> LogViolation(Guid attemptId, [FromBody] ViolationRequest request)
    {
        try
        {
            await assessmentSvc.LogViolationAsync(attemptId, request.ViolationType, UserId);
            return Ok(ApiResponse<object>.Ok(null));
        }
        catch (Exception ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPost("attempts/{attemptId:guid}/submit")]
    public async Task<IActionResult> Submit(Guid attemptId)
    {
        try
        {
            var result = await assessmentSvc.SubmitAttemptAsync(attemptId, UserId);
            return Ok(ApiResponse<object>.Ok(result));
        }
        catch (Exception ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpGet("attempts/{attemptId:guid}/result")]
    public async Task<IActionResult> GetResult(Guid attemptId)
    {
        try
        {
            var result = await assessmentSvc.GetResultAsync(attemptId, UserId);
            return Ok(ApiResponse<object>.Ok(result));
        }
        catch (Exception ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpGet("my-tests")]
    public async Task<IActionResult> MyTests()
    {
        try
        {
            var result = await assessmentSvc.GetAvailableTestsAsync(UserId);
            return Ok(ApiResponse<object>.Ok(result));
        }
        catch (Exception ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }
}
