using GridAcademy.Common;
using GridAcademy.Data.Entities.Exam;
using GridAcademy.Services.ExamContent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GridAcademy.Controllers;

[ApiController]
[Route("api/admin/content")]
[AllowAnonymous]
public class AdminContentController(IContentWorkflowService workflowService) : ControllerBase
{
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveContent(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await workflowService.ApproveAsync(id, ct);
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

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> PublishContent(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await workflowService.PublishAsync(id, ct);
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

    [HttpGet]
    public async Task<IActionResult> ListContent(
        [FromQuery] PublicationStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await workflowService.GetAdminContentAsync(status, page, pageSize, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
