using System.Security.Claims;
using GridAcademy.Common;
using GridAcademy.DTOs.Marketplace;
using GridAcademy.Services.Marketplace;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GridAcademy.Controllers.Marketplace;

[ApiController]
[Route("api/orders")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Create a Razorpay order for a test series purchase.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateOrderResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req, CancellationToken ct)
    {
        var result = await _orders.CreateAsync(CurrentUserId, req, ct);
        return StatusCode(201, ApiResponse<CreateOrderResponse>.Ok(result, "Order created."));
    }

    /// <summary>Verify Razorpay payment signature and grant entitlement.</summary>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest req, CancellationToken ct)
    {
        var success = await _orders.VerifyPaymentAsync(CurrentUserId, req, ct);
        if (!success)
            return BadRequest(ApiResponse.Fail("Payment verification failed. Please contact support."));
        return Ok(ApiResponse.Ok("Payment verified. Access granted."));
    }

    /// <summary>Check if the current student has an active entitlement for a series.</summary>
    [HttpGet("entitlement/{seriesId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<EntitlementCheckResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckEntitlement(Guid seriesId, CancellationToken ct)
    {
        var result = await _orders.CheckEntitlementAsync(CurrentUserId, seriesId, ct);
        return Ok(ApiResponse<EntitlementCheckResponse>.Ok(result));
    }
}
