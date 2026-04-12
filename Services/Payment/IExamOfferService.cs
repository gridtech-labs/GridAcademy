using GridAcademy.DTOs.Payment;

namespace GridAcademy.Services.Payment;

public interface IExamOfferService
{
    Task<ValidateOfferResponse>  ValidateAsync(ValidateOfferRequest req, CancellationToken ct = default);
    Task<List<ExamOfferDto>>     GetActiveOffersForExamAsync(Guid examPageId, CancellationToken ct = default);
    Task<List<ExamOfferDto>>     GetAllOffersAsync(CancellationToken ct = default);
    Task<ExamOfferDto>           SaveAsync(int? id, SaveExamOfferRequest req, CancellationToken ct = default);
    Task                         DeleteAsync(int id, CancellationToken ct = default);
}
