using GridAcademy.Common;
using GridAcademy.DTOs.Marketplace;
using GridAcademy.Services.Marketplace;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GridAcademy.Controllers.Marketplace;

/// <summary>
/// Admin-only marketplace management API.
/// All routes require JWT with role "Admin".
/// </summary>
[ApiController]
[Route("api/marketplace/admin")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class MarketplaceAdminController : ControllerBase
{
    private readonly IMarketplaceAdminService _adminSvc;

    public MarketplaceAdminController(IMarketplaceAdminService adminSvc) => _adminSvc = adminSvc;

    // ── Dashboard ─────────────────────────────────────────────────────────────

    /// <summary>Marketplace KPIs: GMV, orders, providers, pending reviews.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<MarketplaceDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var data = await _adminSvc.GetDashboardAsync(ct);
        return Ok(ApiResponse<MarketplaceDashboardDto>.Ok(data));
    }

    // ── Providers ─────────────────────────────────────────────────────────────

    /// <summary>List all providers with optional status filter (Pending/Verified/Suspended).</summary>
    [HttpGet("providers")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AdminProviderListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var data = await _adminSvc.GetProvidersAsync(page, pageSize, status, ct);
        return Ok(ApiResponse<PagedResult<AdminProviderListDto>>.Ok(data));
    }

    /// <summary>Verify, suspend, or reset a provider status.</summary>
    [HttpPatch("providers/{providerId:int}/status")]
    [ProducesResponseType(typeof(ApiResponse<ProviderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProviderStatus(int providerId,
        [FromQuery] string status,
        [FromQuery] string? notes = null,
        CancellationToken ct = default)
    {
        var data = await _adminSvc.UpdateProviderStatusAsync(providerId, status, notes, ct);
        return Ok(ApiResponse<ProviderDto>.Ok(data, $"Provider status updated to {status}."));
    }

    // ── Review Queue ──────────────────────────────────────────────────────────

    /// <summary>Test series pending admin review.</summary>
    [HttpGet("review-queue")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TestReviewQueueDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReviewQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var data = await _adminSvc.GetReviewQueueAsync(page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<TestReviewQueueDto>>.Ok(data));
    }

    /// <summary>Approve or reject a test series (PendingReview → Published / Rejected).</summary>
    [HttpPost("review-queue/{seriesId:guid}/decision")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReviewSeries(Guid seriesId, [FromBody] ReviewDecisionRequest req, CancellationToken ct)
    {
        await _adminSvc.ReviewSeriesAsync(seriesId, req, ct);
        var msg = req.Approved ? "Series approved and published." : "Series rejected.";
        return Ok(ApiResponse.Ok(msg));
    }

    // ── Commissions ───────────────────────────────────────────────────────────

    /// <summary>View all commissions, optionally filtered by provider or pending status.</summary>
    [HttpGet("commissions")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CommissionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCommissions(
        [FromQuery] int? providerId = null,
        [FromQuery] bool pendingOnly = false,
        CancellationToken ct = default)
    {
        var data = await _adminSvc.GetCommissionsAsync(providerId, pendingOnly, ct);
        return Ok(ApiResponse<IReadOnlyList<CommissionDto>>.Ok(data));
    }

    // ── Payouts ───────────────────────────────────────────────────────────────

    /// <summary>Initiate a payout for a provider — bundles all pending commissions.</summary>
    [HttpPost("payouts")]
    [ProducesResponseType(typeof(ApiResponse<PayoutDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> InitiatePayout([FromBody] InitiatePayoutRequest req, CancellationToken ct)
    {
        var data = await _adminSvc.InitiatePayoutAsync(req, ct);
        return StatusCode(201, ApiResponse<PayoutDto>.Ok(data, "Payout initiated."));
    }

    /// <summary>List all payouts, optionally filtered by provider.</summary>
    [HttpGet("payouts")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PayoutDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayouts([FromQuery] int? providerId = null, CancellationToken ct = default)
    {
        var data = await _adminSvc.GetPayoutsAsync(providerId, ct);
        return Ok(ApiResponse<IReadOnlyList<PayoutDto>>.Ok(data));
    }

    // ── CMS Banners ───────────────────────────────────────────────────────────

    /// <summary>List all homepage banners (including inactive).</summary>
    [HttpGet("banners")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CmsBannerDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBanners(CancellationToken ct)
    {
        var data = await _adminSvc.GetBannersAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<CmsBannerDto>>.Ok(data));
    }

    /// <summary>Create a homepage banner.</summary>
    [HttpPost("banners")]
    [ProducesResponseType(typeof(ApiResponse<CmsBannerDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBanner([FromBody] CreateBannerRequest req, CancellationToken ct)
    {
        var data = await _adminSvc.CreateBannerAsync(req, ct);
        return StatusCode(201, ApiResponse<CmsBannerDto>.Ok(data, "Banner created."));
    }

    /// <summary>Update a homepage banner.</summary>
    [HttpPut("banners/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CmsBannerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBanner(int id, [FromBody] UpdateBannerRequest req, CancellationToken ct)
    {
        var data = await _adminSvc.UpdateBannerAsync(id, req, ct);
        return Ok(ApiResponse<CmsBannerDto>.Ok(data, "Banner updated."));
    }

    /// <summary>Delete a homepage banner.</summary>
    [HttpDelete("banners/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteBanner(int id, CancellationToken ct)
    {
        await _adminSvc.DeleteBannerAsync(id, ct);
        return Ok(ApiResponse.Ok("Banner deleted."));
    }

    // ── Promo Codes ───────────────────────────────────────────────────────────

    /// <summary>List all promo codes.</summary>
    [HttpGet("promo-codes")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PromoCodeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPromoCodes(CancellationToken ct)
    {
        var data = await _adminSvc.GetPromoCodesAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<PromoCodeDto>>.Ok(data));
    }

    /// <summary>Create a promo code.</summary>
    [HttpPost("promo-codes")]
    [ProducesResponseType(typeof(ApiResponse<PromoCodeDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePromoCode([FromBody] CreatePromoCodeRequest req, CancellationToken ct)
    {
        var data = await _adminSvc.CreatePromoCodeAsync(req, ct);
        return StatusCode(201, ApiResponse<PromoCodeDto>.Ok(data, "Promo code created."));
    }

    /// <summary>Activate or deactivate a promo code.</summary>
    [HttpPatch("promo-codes/{id:int}/toggle")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> TogglePromoCode(int id, [FromQuery] bool isActive, CancellationToken ct)
    {
        await _adminSvc.TogglePromoCodeAsync(id, isActive, ct);
        return Ok(ApiResponse.Ok($"Promo code {(isActive ? "activated" : "deactivated")}."));
    }
}
