using System.Security.Claims;
using GridAcademy.Common;
using GridAcademy.DTOs.Marketplace;
using GridAcademy.Services.Marketplace;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GridAcademy.Controllers.Marketplace;

/// <summary>
/// Student-facing marketplace endpoints.
/// All routes require a valid JWT with role "Student".
/// </summary>
[ApiController]
[Route("api/student")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Student")]
public class StudentController : ControllerBase
{
    private readonly IStudentService _students;

    public StudentController(IStudentService students) => _students = students;

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Student dashboard — stats, purchased series, recent orders.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<StudentDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var data = await _students.GetDashboardAsync(CurrentUserId, ct);
        return Ok(ApiResponse<StudentDashboardDto>.Ok(data));
    }

    /// <summary>All series the student has access to (via entitlement).</summary>
    [HttpGet("series")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PurchasedSeriesDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTests(CancellationToken ct)
    {
        var data = await _students.GetPurchasedSeriesAsync(CurrentUserId, ct);
        return Ok(ApiResponse<IReadOnlyList<PurchasedSeriesDto>>.Ok(data));
    }

    /// <summary>Order history for the student.</summary>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(CancellationToken ct)
    {
        var data = await _students.GetOrdersAsync(CurrentUserId, ct);
        return Ok(ApiResponse<IReadOnlyList<OrderDto>>.Ok(data));
    }

    /// <summary>Submit a rating and review for a purchased series.</summary>
    [HttpPost("reviews")]
    [ProducesResponseType(typeof(ApiResponse<ReviewDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewRequest req, CancellationToken ct)
    {
        var review = await _students.SubmitReviewAsync(CurrentUserId, req, ct);
        return StatusCode(201, ApiResponse<ReviewDto>.Ok(review, "Review submitted."));
    }
}
