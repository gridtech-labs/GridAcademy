using GridAcademy.Common;
using GridAcademy.DTOs.Content.Masters;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GridAcademy.Controllers;

/// <summary>Manages master / lookup data for the content module.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class MastersController : ControllerBase
{
    private readonly IMasterService _svc;
    public MastersController(IMasterService svc) => _svc = svc;

    // ── Question Types ────────────────────────────────────────────────────────

    [HttpGet("question-types")]
    public async Task<IActionResult> GetQuestionTypes([FromQuery] bool activeOnly = false) =>
        Ok(ApiResponse<object>.Ok(await _svc.GetQuestionTypesAsync(activeOnly)));

    [HttpPut("question-types/{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> UpdateQuestionType(int id, [FromBody] UpdateQuestionTypeRequest req)
    {
        try { return Ok(ApiResponse<object>.Ok(await _svc.UpdateQuestionTypeAsync(id, req.Name, req.Description, req.IsActive))); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
    }

    // ── Subjects ─────────────────────────────────────────────────────────────

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects([FromQuery] bool activeOnly = true) =>
        Ok(ApiResponse<object>.Ok(await _svc.GetSubjectsAsync(activeOnly)));

    [HttpGet("subjects/{id:int}")]
    public async Task<IActionResult> GetSubject(int id)
    {
        try { return Ok(ApiResponse<object>.Ok(await _svc.GetSubjectAsync(id))); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPost("subjects")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> CreateSubject([FromBody] CreateMasterRequest request)
    {
        var dto = await _svc.CreateSubjectAsync(request);
        return CreatedAtAction(nameof(GetSubject), new { id = dto.Id }, ApiResponse<object>.Ok(dto));
    }

    [HttpPut("subjects/{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> UpdateSubject(int id, [FromBody] CreateMasterRequest request)
    {
        try { return Ok(ApiResponse<object>.Ok(await _svc.UpdateSubjectAsync(id, request))); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
    }

    [HttpDelete("subjects/{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> DeleteSubject(int id)
    {
        try { await _svc.DeleteSubjectAsync(id); return Ok(ApiResponse<object>.Ok("Deleted.")); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
    }

    // ── Topics ───────────────────────────────────────────────────────────────

    [HttpGet("topics")]
    public async Task<IActionResult> GetTopics([FromQuery] int? subjectId, [FromQuery] bool activeOnly = true) =>
        Ok(ApiResponse<object>.Ok(await _svc.GetTopicsAsync(subjectId, activeOnly)));

    [HttpPost("topics")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> CreateTopic([FromBody] CreateTopicRequest request)
    {
        try { return Ok(ApiResponse<object>.Ok(await _svc.CreateTopicAsync(request))); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPut("topics/{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> UpdateTopic(int id, [FromBody] CreateTopicRequest request)
    {
        try { return Ok(ApiResponse<object>.Ok(await _svc.UpdateTopicAsync(id, request))); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
    }

    [HttpDelete("topics/{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> DeleteTopic(int id)
    {
        try { await _svc.DeleteTopicAsync(id); return Ok(ApiResponse<object>.Ok("Deleted.")); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
    }

    // ── Lookup lists (read-only for all authenticated users) ─────────────────

    [HttpGet("difficulty-levels")]
    public async Task<IActionResult> GetDifficultyLevels() =>
        Ok(ApiResponse<object>.Ok(await _svc.GetDifficultyLevelsAsync()));

    [HttpPost("difficulty-levels")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> CreateDifficultyLevel([FromBody] CreateMasterRequest r) =>
        Ok(ApiResponse<object>.Ok(await _svc.CreateDifficultyLevelAsync(r)));

    [HttpGet("complexity-levels")]
    public async Task<IActionResult> GetComplexityLevels() =>
        Ok(ApiResponse<object>.Ok(await _svc.GetComplexityLevelsAsync()));

    [HttpPost("complexity-levels")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> CreateComplexityLevel([FromBody] CreateMasterRequest r) =>
        Ok(ApiResponse<object>.Ok(await _svc.CreateComplexityLevelAsync(r)));

    [HttpGet("exam-types")]
    public async Task<IActionResult> GetExamTypes() =>
        Ok(ApiResponse<object>.Ok(await _svc.GetExamTypesAsync()));

    [HttpPost("exam-types")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> CreateExamType([FromBody] CreateMasterRequest r) =>
        Ok(ApiResponse<object>.Ok(await _svc.CreateExamTypeAsync(r)));

    [HttpGet("tags")]
    public async Task<IActionResult> GetTags() =>
        Ok(ApiResponse<object>.Ok(await _svc.GetTagsAsync()));

    [HttpPost("tags")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> CreateTag([FromBody] CreateMasterRequest r) =>
        Ok(ApiResponse<object>.Ok(await _svc.CreateTagAsync(r)));

    [HttpGet("marks")]
    public async Task<IActionResult> GetMarks() =>
        Ok(ApiResponse<object>.Ok(await _svc.GetMarksAsync()));

    [HttpPost("marks")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> CreateMarks([FromBody] CreateMarksRequest r) =>
        Ok(ApiResponse<object>.Ok(await _svc.CreateMarksAsync(r)));

    [HttpGet("negative-marks")]
    public async Task<IActionResult> GetNegativeMarks() =>
        Ok(ApiResponse<object>.Ok(await _svc.GetNegativeMarksAsync()));

    [HttpPost("negative-marks")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> CreateNegativeMarks([FromBody] CreateMarksRequest r) =>
        Ok(ApiResponse<object>.Ok(await _svc.CreateNegativeMarksAsync(r)));
}
