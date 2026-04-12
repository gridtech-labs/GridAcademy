using GridAcademy.Data.Entities.Payment;
using GridAcademy.DTOs.Payment;

namespace GridAcademy.Services.Payment;

public interface IExamPaymentService
{
    Task<CreateExamOrderResponse>  CreateOrderAsync(Guid studentId, CreateExamOrderRequest req, CancellationToken ct = default);
    Task<bool>                     VerifyPaymentAsync(Guid studentId, VerifyExamPaymentRequest req, CancellationToken ct = default);
    Task<ExamAccessResponse>       CheckAccessAsync(Guid studentId, Guid examPageId, CancellationToken ct = default);
    Task<List<ExamOrderListItem>>  GetOrdersAsync(ExamOrderStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task<ExamOrderDetail?>         GetOrderDetailAsync(Guid orderId, CancellationToken ct = default);
    Task<bool>                     ProcessWebhookAsync(string payload, string signature, CancellationToken ct = default);
}
