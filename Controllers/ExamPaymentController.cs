using System.Security.Claims;
using GridAcademy.Common;
using GridAcademy.DTOs.Payment;
using GridAcademy.Services.Payment;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GridAcademy.Controllers;

[ApiController]
[Route("api/exam-payment")]
[Produces("application/json")]
public class ExamPaymentController : ControllerBase
{
    private readonly IExamPaymentService _payment;
    private readonly IExamOfferService   _offers;

    public ExamPaymentController(IExamPaymentService payment, IExamOfferService offers)
    {
        _payment = payment;
        _offers  = offers;
    }

    // Read "sub" first (what JwtHelper writes), fall back to NameIdentifier
    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue("sub") ??
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        throw new UnauthorizedAccessException("User identity not found."));

    // ── Create Order ──────────────────────────────────────────────────────────
    [HttpPost("create-order")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(ApiResponse<CreateExamOrderResponse>), 201)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateExamOrderRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _payment.CreateOrderAsync(CurrentUserId, req, ct);
            return StatusCode(201, ApiResponse<CreateExamOrderResponse>.Ok(result, "Order created."));
        }
        catch (KeyNotFoundException ex)        { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex)   { return BadRequest(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ApiResponse.Fail(ex.Message)); }
    }

    // ── Verify Payment ────────────────────────────────────────────────────────
    [HttpPost("verify")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyExamPaymentRequest req, CancellationToken ct)
    {
        try
        {
            var success = await _payment.VerifyPaymentAsync(CurrentUserId, req, ct);
            if (!success)
                return BadRequest(ApiResponse.Fail("Payment verification failed. Please contact support."));
            return Ok(ApiResponse.Ok("Payment verified. Access granted."));
        }
        catch (KeyNotFoundException ex)        { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex)   { return BadRequest(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ApiResponse.Fail(ex.Message)); }
    }

    // ── Check Access ──────────────────────────────────────────────────────────
    [HttpGet("access/{examPageId:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(ApiResponse<ExamAccessResponse>), 200)]
    public async Task<IActionResult> CheckAccess(Guid examPageId, CancellationToken ct)
    {
        try
        {
            var result = await _payment.CheckAccessAsync(CurrentUserId, examPageId, ct);
            return Ok(ApiResponse<ExamAccessResponse>.Ok(result));
        }
        catch (Exception ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    // ── Active Offers for an Exam (public) ───────────────────────────────────
    [HttpGet("offers/{examPageId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<ExamOfferDto>>), 200)]
    public async Task<IActionResult> GetOffers(Guid examPageId, CancellationToken ct)
    {
        try
        {
            var result = await _offers.GetActiveOffersForExamAsync(examPageId, ct);
            return Ok(ApiResponse<List<ExamOfferDto>>.Ok(result));
        }
        catch (Exception ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    // ── Validate Offer ────────────────────────────────────────────────────────
    [HttpPost("offers/validate")]
    [ProducesResponseType(typeof(ApiResponse<ValidateOfferResponse>), 200)]
    public async Task<IActionResult> ValidateOffer([FromBody] ValidateOfferRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _offers.ValidateAsync(req, ct);
            return Ok(ApiResponse<ValidateOfferResponse>.Ok(result));
        }
        catch (Exception ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    // ── Webhook (no auth — verified by signature) ─────────────────────────────
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var payload   = await reader.ReadToEndAsync(ct);
        var signature = Request.Headers["X-Razorpay-Signature"].FirstOrDefault() ?? "";

        var ok = await _payment.ProcessWebhookAsync(payload, signature, ct);
        return ok ? Ok() : BadRequest();
    }
}
