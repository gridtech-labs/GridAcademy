using System.Security.Claims;
using GridAcademy.Common;
using GridAcademy.DTOs.Marketplace;
using GridAcademy.Services.Marketplace;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GridAcademy.Controllers.Marketplace;

/// <summary>
/// Provider portal — create and manage test series, view commissions.
/// All routes require a valid JWT with role "Provider".
/// </summary>
[ApiController]
[Route("api/provider")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Provider")]
public class ProviderController : ControllerBase
{
    private readonly IProviderService _providers;

    public ProviderController(IProviderService providers) => _providers = providers;

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── Profile ───────────────────────────────────────────────────────────────

    /// <summary>Get provider profile.</summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ApiResponse<ProviderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var data = await _providers.GetProfileAsync(CurrentUserId, ct);
        return Ok(ApiResponse<ProviderDto>.Ok(data));
    }

    /// <summary>Update provider profile (institute name, city, bio, logo).</summary>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(ApiResponse<ProviderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProviderProfileRequest req, CancellationToken ct)
    {
        var data = await _providers.UpdateProfileAsync(CurrentUserId, req, ct);
        return Ok(ApiResponse<ProviderDto>.Ok(data, "Profile updated."));
    }

    /// <summary>Provider dashboard — series summary, revenue, top performers.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<ProviderDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var data = await _providers.GetDashboardAsync(CurrentUserId, ct);
        return Ok(ApiResponse<ProviderDashboardDto>.Ok(data));
    }

    // ── Test Series ───────────────────────────────────────────────────────────

    /// <summary>List all series belonging to this provider.</summary>
    [HttpGet("series")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TestSeriesListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySeries([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var data = await _providers.GetMySeriesAsync(CurrentUserId, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<TestSeriesListDto>>.Ok(data));
    }

    /// <summary>Create a new test series (starts as Draft).</summary>
    [HttpPost("series")]
    [ProducesResponseType(typeof(ApiResponse<TestSeriesListDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSeries([FromBody] CreateTestSeriesRequest req, CancellationToken ct)
    {
        var data = await _providers.CreateSeriesAsync(CurrentUserId, req, ct);
        return StatusCode(201, ApiResponse<TestSeriesListDto>.Ok(data, "Series created."));
    }

    /// <summary>Update an existing Draft series.</summary>
    [HttpPut("series/{seriesId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TestSeriesListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSeries(Guid seriesId, [FromBody] UpdateTestSeriesRequest req, CancellationToken ct)
    {
        var data = await _providers.UpdateSeriesAsync(CurrentUserId, seriesId, req, ct);
        return Ok(ApiResponse<TestSeriesListDto>.Ok(data, "Series updated."));
    }

    /// <summary>Delete a Draft series.</summary>
    [HttpDelete("series/{seriesId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteSeries(Guid seriesId, CancellationToken ct)
    {
        await _providers.DeleteSeriesAsync(CurrentUserId, seriesId, ct);
        return Ok(ApiResponse.Ok("Series deleted."));
    }

    /// <summary>Submit a series for admin review (Draft → PendingReview).</summary>
    [HttpPost("series/{seriesId:guid}/submit")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitForReview(Guid seriesId, CancellationToken ct)
    {
        await _providers.SubmitForReviewAsync(CurrentUserId, seriesId, ct);
        return Ok(ApiResponse.Ok("Series submitted for review."));
    }

    // ── Tests in a Series ─────────────────────────────────────────────────────

    /// <summary>Add an existing published test to a series.</summary>
    [HttpPost("series/{seriesId:guid}/tests")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddTest(Guid seriesId,
        [FromQuery] Guid testId,
        [FromQuery] int sortOrder = 0,
        [FromQuery] bool isFreePreview = false,
        CancellationToken ct = default)
    {
        await _providers.AddTestToSeriesAsync(CurrentUserId, seriesId, testId, sortOrder, isFreePreview, ct);
        return StatusCode(201, ApiResponse.Ok("Test added to series."));
    }

    /// <summary>Remove a test from a series.</summary>
    [HttpDelete("series/{seriesId:guid}/tests/{testId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveTest(Guid seriesId, Guid testId, CancellationToken ct)
    {
        await _providers.RemoveTestFromSeriesAsync(CurrentUserId, seriesId, testId, ct);
        return Ok(ApiResponse.Ok("Test removed from series."));
    }

    // ── Commissions ───────────────────────────────────────────────────────────

    /// <summary>Commission ledger for this provider.</summary>
    [HttpGet("commissions")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CommissionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCommissions(CancellationToken ct)
    {
        var data = await _providers.GetCommissionsAsync(CurrentUserId, ct);
        return Ok(ApiResponse<IReadOnlyList<CommissionDto>>.Ok(data));
    }
}
