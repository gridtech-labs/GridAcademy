using GridAcademy.Common;
using GridAcademy.DTOs.Exam;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;

namespace GridAcademy.Controllers;

[ApiController]
[Route("api/exams")]
public class ExamController(IExamService svc) : ControllerBase
{
    // ── Public Endpoints ──────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? levelId, [FromQuery] string? search)
    {
        var items = await svc.GetExamPagesAsync(activeOnly: true, levelId, search);
        return Ok(ApiResponse<object>.Ok(items));
    }

    [HttpGet("levels")]
    public async Task<IActionResult> Levels()
    {
        var items = await svc.GetExamLevelsAsync();
        return Ok(ApiResponse<object>.Ok(items));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail(string slug)
    {
        var item = await svc.GetExamBySlugAsync(slug, incrementView: true);
        if (item == null) return NotFound(ApiResponse<object>.Fail("Exam not found."));
        return Ok(ApiResponse<object>.Ok(item));
    }

    // ── Admin Endpoints (JWT) ─────────────────────────────────────────────

    [HttpGet("admin/all")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> AdminList([FromQuery] int? levelId, [FromQuery] string? search)
    {
        var items = await svc.GetExamPagesAsync(activeOnly: false, levelId, search);
        return Ok(ApiResponse<object>.Ok(items));
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> Create([FromBody] SaveExamPageRequest request)
    {
        try
        {
            var result = await svc.CreateExamAsync(request);
            return Ok(ApiResponse<object>.Ok(result));
        }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
    }

    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveExamPageRequest request)
    {
        try
        {
            var result = await svc.UpdateExamAsync(id, request);
            return Ok(ApiResponse<object>.Ok(result));
        }
        catch (KeyNotFoundException) { return NotFound(ApiResponse<object>.Fail("Not found.")); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try { await svc.DeleteExamAsync(id); return Ok(ApiResponse<object>.Ok(null)); }
        catch (KeyNotFoundException) { return NotFound(ApiResponse<object>.Fail("Not found.")); }
    }

    // ── Test mapping ───────────────────────────────────────────────────────

    [HttpPost("{id:guid}/tests")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> MapTest(Guid id, [FromBody] MapTestRequest request)
    {
        try { await svc.MapTestAsync(id, request); return Ok(ApiResponse<object>.Ok(null)); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(ex.Message)); }
    }

    [HttpDelete("{id:guid}/tests/{testId:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> UnmapTest(Guid id, Guid testId)
    {
        await svc.UnmapTestAsync(id, testId);
        return Ok(ApiResponse<object>.Ok(null));
    }

    // ── Exam Level admin ──────────────────────────────────────────────────

    [HttpPost("levels")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> SaveLevel([FromQuery] int? id, [FromBody] SaveExamLevelRequest request)
    {
        var result = await svc.SaveExamLevelAsync(id, request);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpDelete("levels/{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> DeleteLevel(int id)
    {
        try { await svc.DeleteExamLevelAsync(id); return Ok(ApiResponse<object>.Ok(null)); }
        catch (KeyNotFoundException) { return NotFound(ApiResponse<object>.Fail("Not found.")); }
    }
}
