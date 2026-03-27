using System.Security.Claims;
using GridAcademy.Common;
using GridAcademy.Data.Entities.Content;
using GridAcademy.DTOs.Content.Questions;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GridAcademy.Controllers;

/// <summary>CRUD and workflow for exam questions.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _svc;
    public QuestionsController(IQuestionService svc) => _svc = svc;

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    // ── List ─────────────────────────────────────────────────────────────────

    /// <summary>List questions with optional filters and pagination.</summary>
    [HttpGet]
    public async Task<IActionResult> GetQuestions([FromQuery] QuestionListRequest request) =>
        Ok(ApiResponse<object>.Ok(await _svc.GetQuestionsAsync(request)));

    // ── Get Single ───────────────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetQuestion(Guid id)
    {
        try { return Ok(ApiResponse<object>.Ok(await _svc.GetByIdAsync(id))); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
    }

    // ── Create (Admin or Instructor) ─────────────────────────────────────────

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionRequest request)
    {
        try
        {
            var dto = await _svc.CreateAsync(request, CurrentUserId);
            return CreatedAtAction(nameof(GetQuestion), new { id = dto.Id }, ApiResponse<object>.Ok(dto));
        }
        catch (ArgumentException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    // ── Update ───────────────────────────────────────────────────────────────

    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> UpdateQuestion(Guid id, [FromBody] UpdateQuestionRequest request)
    {
        try { return Ok(ApiResponse<object>.Ok(await _svc.UpdateAsync(id, request, CurrentUserId))); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    // ── Publish / Unpublish ──────────────────────────────────────────────────

    [HttpPost("{id:guid}/publish")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> Publish(Guid id)
    {
        try { await _svc.PublishAsync(id); return Ok(ApiResponse<object>.Ok("Question published.")); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPost("{id:guid}/unpublish")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> Unpublish(Guid id)
    {
        try { await _svc.UnpublishAsync(id); return Ok(ApiResponse<object>.Ok("Question moved back to Draft.")); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
    }

    // ── Delete (Admin only) ──────────────────────────────────────────────────

    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> DeleteQuestion(Guid id)
    {
        try { await _svc.DeleteAsync(id); return Ok(ApiResponse<object>.Ok("Deleted.")); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
    }
}
