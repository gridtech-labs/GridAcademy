using GridAcademy.Common;
using GridAcademy.DTOs.Marketplace;

namespace GridAcademy.Services.Marketplace;

public interface IStorefrontService
{
    Task<HomepageDto>                         GetHomepageAsync(CancellationToken ct = default);
    Task<PagedResult<TestSeriesListDto>>      SearchSeriesAsync(TestSeriesSearchRequest req, CancellationToken ct = default);
    Task<TestSeriesDetailDto>                 GetSeriesDetailAsync(string slug, CancellationToken ct = default);
    Task<PromoCodeValidateResponse>           ValidatePromoCodeAsync(PromoCodeValidateRequest req, CancellationToken ct = default);
}
