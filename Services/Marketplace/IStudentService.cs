using GridAcademy.DTOs.Marketplace;

namespace GridAcademy.Services.Marketplace;

public interface IStudentService
{
    Task<StudentDashboardDto>             GetDashboardAsync(Guid studentId, CancellationToken ct = default);
    Task<IReadOnlyList<PurchasedSeriesDto>> GetPurchasedSeriesAsync(Guid studentId, CancellationToken ct = default);
    Task<IReadOnlyList<OrderDto>>         GetOrdersAsync(Guid studentId, CancellationToken ct = default);
    Task<ReviewDto>                       SubmitReviewAsync(Guid studentId, SubmitReviewRequest req, CancellationToken ct = default);
}
