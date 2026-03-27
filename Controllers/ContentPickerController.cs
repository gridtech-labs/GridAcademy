using GridAcademy.Common;
using GridAcademy.Data;
using GridAcademy.Data.Entities.VideoLearning;
using GridAcademy.DTOs.VideoLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GridAcademy.Controllers;

/// <summary>
/// Returns content items for the type-filtered picker dropdown in Learning Path builder.
/// Accessible to Admin and Instructor via cookie auth (admin panel usage).
/// </summary>
[ApiController]
[Route("api/content-picker")]
[Authorize(Roles = "Admin,Instructor")]
public class ContentPickerController(AppDbContext db) : ControllerBase
{
    [HttpGet("videos")]
    public async Task<IActionResult> GetVideos([FromQuery] int? domainId, [FromQuery] string? search)
    {
        var q = db.VlVideos.Where(v => v.Status == VideoStatus.Published || v.Status == VideoStatus.Draft);
        if (domainId.HasValue) q = q.Where(v => v.DomainId == domainId.Value);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(v => v.Title.ToLower().Contains(search.ToLower()));

        var items = await q.OrderBy(v => v.Title)
            .Select(v => new ContentPickerItem(v.Id, v.Title,
                v.Category.Name + " · " + (v.DurationSeconds / 60) + " min",
                v.DurationSeconds))
            .Take(100).ToListAsync();

        return Ok(ApiResponse<object>.Ok(items));
    }

    [HttpGet("assessments")]
    public async Task<IActionResult> GetAssessments([FromQuery] string? search,
        [FromQuery] bool publishedOnly = false)
    {
        var q = db.Set<GridAcademy.Data.Entities.Assessment.Test>().AsQueryable();

        // By default show Published tests; pass publishedOnly=false to see all
        if (publishedOnly)
            q = q.Where(t => t.Status == GridAcademy.Data.Entities.Assessment.TestStatus.Published);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(t => t.Title.ToLower().Contains(search.ToLower()));

        var items = await q.OrderBy(t => t.Title)
            .Select(t => new ContentPickerItem(t.Id, t.Title,
                t.Status.ToString(), null))
            .Take(100).ToListAsync();

        return Ok(ApiResponse<object>.Ok(items));
    }

    [HttpGet("content-files")]
    public async Task<IActionResult> GetContentFiles([FromQuery] int? domainId,
        [FromQuery] ContentFileType? fileType, [FromQuery] string? search)
    {
        var q = db.VlContentFiles.Where(f => f.IsActive);
        if (domainId.HasValue)  q = q.Where(f => f.DomainId == domainId.Value);
        if (fileType.HasValue)  q = q.Where(f => f.ContentType == fileType.Value);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(f => f.Title.ToLower().Contains(search.ToLower()));

        var items = await q.OrderBy(f => f.Title)
            .Select(f => new ContentPickerItem(f.Id, f.Title,
                f.ContentType.ToString(), null))
            .Take(100).ToListAsync();

        return Ok(ApiResponse<object>.Ok(items));
    }
}
