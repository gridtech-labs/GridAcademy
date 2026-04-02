using GridAcademy.Common;
using GridAcademy.Data.Entities.Exam;
using GridAcademy.DTOs.ExamContent;
using GridAcademy.Services.ExamContent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;

namespace GridAcademy.Controllers;

[ApiController]
[Route("api/exam-content")]
public class ExamContentController(IExamContentService service) : ControllerBase
{
    [HttpPost("exams")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> CreateExam([FromBody] CreateExamRequest request, CancellationToken ct)
    {
        try
        {
            var result = await service.CreateExamAsync(request, ct);
            return Ok(ApiResponse<object>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("exams")]
    public async Task<IActionResult> GetExams([FromQuery] string? category, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await service.GetExamsAsync(category, page, pageSize, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("exams/{slug}")]
    public async Task<IActionResult> GetExamBySlug(string slug, CancellationToken ct)
    {
        var result = await service.GetExamBySlugAsync(slug, ct);
        return result is null
            ? NotFound(ApiResponse<object>.Fail("Exam not found."))
            : Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("notifications")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> CreateNotification([FromBody] CreateExamNotificationRequest request, CancellationToken ct)
    {
        try
        {
            var result = await service.CreateNotificationAsync(request, ct);
            return Ok(ApiResponse<object>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] Guid? examId,
        [FromQuery] string? category,
        [FromQuery] ExamNotificationType? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await service.GetNotificationsAsync(examId, category, type, page, pageSize, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("notifications/{slug}")]
    public async Task<IActionResult> GetNotificationBySlug(string slug, CancellationToken ct)
    {
        var result = await service.GetNotificationBySlugAsync(slug, ct);
        return result is null
            ? NotFound(ApiResponse<object>.Fail("Notification not found."))
            : Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPut("notifications/{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> UpdateNotification(Guid id, [FromBody] UpdateExamNotificationRequest request, CancellationToken ct)
    {
        try
        {
            var result = await service.UpdateNotificationAsync(id, request, ct);
            return Ok(ApiResponse<object>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("notifications/{id:guid}/publish")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> PublishNotification(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await service.ChangeStatusAsync(id, PublicationStatus.Published, ct);
            return Ok(ApiResponse<object>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("notifications/{id:guid}/draft")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> DraftNotification(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await service.ChangeStatusAsync(id, PublicationStatus.Draft, ct);
            return Ok(ApiResponse<object>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("notifications/{id:guid}/versions")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> GetVersionHistory(Guid id, CancellationToken ct)
    {
        var result = await service.GetVersionHistoryAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
