using GridAcademy.DTOs.CareerGuide;

namespace GridAcademy.Services.CareerGuide;

public interface ICareerGuideService
{
    Task<List<CareerQuizQuestionDto>> GetActiveQuestionsAsync(CancellationToken ct = default);
    Task<List<CareerQuizQuestionDto>> GetAllQuestionsAsync(CancellationToken ct = default);
    Task<CareerQuizQuestionDto> CreateQuestionAsync(CreateQuizQuestionRequest req, CancellationToken ct = default);
    Task<bool> UpdateQuestionAsync(int id, UpdateQuizQuestionRequest req, CancellationToken ct = default);
    Task<bool> DeleteQuestionAsync(int id, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, CancellationToken ct = default);
}
