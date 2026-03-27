using GridAcademy.Common;
using GridAcademy.DTOs.Marketplace;
using GridAcademy.Services.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;

namespace GridAcademy.Controllers.Marketplace;

/// <summary>
/// Public storefront API — no authentication required.
/// Powers the Next.js homepage, browse, and test detail pages.
/// </summary>
[ApiController]
[Route("api/storefront")]
[Produces("application/json")]
[AllowAnonymous]
public class StorefrontController : ControllerBase
{
    private readonly IStorefrontService _storefront;

    public StorefrontController(IStorefrontService storefront) => _storefront = storefront;

    /// <summary>Homepage sections: banners, exam categories, featured series.</summary>
    [HttpGet("home")]
    [ProducesResponseType(typeof(ApiResponse<HomepageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHomepage(CancellationToken ct)
    {
        var data = await _storefront.GetHomepageAsync(ct);
        return Ok(ApiResponse<HomepageDto>.Ok(data));
    }

    /// <summary>Search and filter published test series.</summary>
    [HttpGet("tests")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TestSeriesListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchTests([FromQuery] TestSeriesSearchRequest req, CancellationToken ct)
    {
        var data = await _storefront.SearchSeriesAsync(req, ct);
        return Ok(ApiResponse<PagedResult<TestSeriesListDto>>.Ok(data));
    }

    /// <summary>Full detail page for a published test series by slug.</summary>
    [HttpGet("tests/{slug}")]
    [ProducesResponseType(typeof(ApiResponse<TestSeriesDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTestDetail(string slug, CancellationToken ct)
    {
        var data = await _storefront.GetSeriesDetailAsync(slug, ct);
        return Ok(ApiResponse<TestSeriesDetailDto>.Ok(data));
    }

    /// <summary>Validate a promo code before checkout.</summary>
    [HttpPost("promo/validate")]
    [ProducesResponseType(typeof(ApiResponse<PromoCodeValidateResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidatePromo([FromBody] PromoCodeValidateRequest req, CancellationToken ct)
    {
        var data = await _storefront.ValidatePromoCodeAsync(req, ct);
        return Ok(ApiResponse<PromoCodeValidateResponse>.Ok(data));
    }
}
