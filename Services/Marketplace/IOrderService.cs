using GridAcademy.DTOs.Marketplace;

namespace GridAcademy.Services.Marketplace;

public interface IOrderService
{
    Task<CreateOrderResponse>      CreateAsync(Guid studentId, CreateOrderRequest req, CancellationToken ct = default);
    Task<bool>                     VerifyPaymentAsync(Guid studentId, VerifyPaymentRequest req, CancellationToken ct = default);
    Task<EntitlementCheckResponse> CheckEntitlementAsync(Guid studentId, Guid seriesId, CancellationToken ct = default);
}
