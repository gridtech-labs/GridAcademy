using GridAcademy.Common;
using GridAcademy.DTOs.Marketplace;

namespace GridAcademy.Services.Marketplace;

public interface IProviderService
{
    Task<ProviderDto>                        GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<ProviderDto>                        UpdateProfileAsync(Guid userId, UpdateProviderProfileRequest req, CancellationToken ct = default);
    Task<ProviderDashboardDto>               GetDashboardAsync(Guid userId, CancellationToken ct = default);

    // Test Series CRUD
    Task<TestSeriesListDto>                  CreateSeriesAsync(Guid userId, CreateTestSeriesRequest req, CancellationToken ct = default);
    Task<TestSeriesListDto>                  UpdateSeriesAsync(Guid userId, Guid seriesId, UpdateTestSeriesRequest req, CancellationToken ct = default);
    Task                                     DeleteSeriesAsync(Guid userId, Guid seriesId, CancellationToken ct = default);
    Task<PagedResult<TestSeriesListDto>>     GetMySeriesAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task                                     SubmitForReviewAsync(Guid userId, Guid seriesId, CancellationToken ct = default);

    // Series → Test assignments
    Task                                     AddTestToSeriesAsync(Guid userId, Guid seriesId, Guid testId, int sortOrder, bool isFreePreview, CancellationToken ct = default);
    Task                                     RemoveTestFromSeriesAsync(Guid userId, Guid seriesId, Guid testId, CancellationToken ct = default);

    // Commissions
    Task<IReadOnlyList<CommissionDto>>       GetCommissionsAsync(Guid userId, CancellationToken ct = default);
}
