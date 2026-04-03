using GridAcademy.Common;
using GridAcademy.Services.ExamContent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GridAcademy.Controllers;

[ApiController]
[Route("api/content")]
[AllowAnonymous]
public class PublicContentController(IContentWorkflowService workflowService) : ControllerBase
{
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await workflowService.GetPublishedBySlugAsync(slug, ct);
        return result is null
            ? NotFound(ApiResponse<object>.Fail("Content not found."))
            : Ok(ApiResponse<object>.Ok(result));
    }
}
