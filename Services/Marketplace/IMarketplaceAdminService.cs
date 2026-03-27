using GridAcademy.Common;
using GridAcademy.DTOs.Marketplace;

namespace GridAcademy.Services.Marketplace;

public interface IMarketplaceAdminService
{
    Task<MarketplaceDashboardDto>              GetDashboardAsync(CancellationToken ct = default);
    Task<PagedResult<AdminProviderListDto>>    GetProvidersAsync(int page, int pageSize, string? status, CancellationToken ct = default);
    Task<ProviderDto>                          UpdateProviderStatusAsync(int providerId, string status, string? notes, CancellationToken ct = default);
    Task<PagedResult<TestReviewQueueDto>>      GetReviewQueueAsync(int page, int pageSize, CancellationToken ct = default);
    Task                                       ReviewSeriesAsync(Guid seriesId, ReviewDecisionRequest req, CancellationToken ct = default);
    Task<IReadOnlyList<CommissionDto>>         GetCommissionsAsync(int? providerId, bool pendingOnly, CancellationToken ct = default);
    Task<PayoutDto>                            InitiatePayoutAsync(InitiatePayoutRequest req, CancellationToken ct = default);
    Task<IReadOnlyList<PayoutDto>>             GetPayoutsAsync(int? providerId, CancellationToken ct = default);

    // Banners
    Task<CmsBannerDto>                         CreateBannerAsync(CreateBannerRequest req, CancellationToken ct = default);
    Task<CmsBannerDto>                         UpdateBannerAsync(int id, UpdateBannerRequest req, CancellationToken ct = default);
    Task                                       DeleteBannerAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<CmsBannerDto>>          GetBannersAsync(CancellationToken ct = default);

    // Promo Codes
    Task<PromoCodeDto>                         CreatePromoCodeAsync(CreatePromoCodeRequest req, CancellationToken ct = default);
    Task                                       TogglePromoCodeAsync(int id, bool isActive, CancellationToken ct = default);
    Task<IReadOnlyList<PromoCodeDto>>          GetPromoCodesAsync(CancellationToken ct = default);
}
