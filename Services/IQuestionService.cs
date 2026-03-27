using GridAcademy.Common;
using GridAcademy.Data.Entities.Content;
using GridAcademy.DTOs.Content.Questions;

namespace GridAcademy.Services;

public interface IQuestionService
{
    Task<PagedResult<QuestionDto>> GetQuestionsAsync(QuestionListRequest request);
    Task<QuestionDto>              GetByIdAsync(Guid id);
    Task<QuestionDto>              CreateAsync(CreateQuestionRequest request, Guid? createdBy = null);
    Task<QuestionDto>              UpdateAsync(Guid id, UpdateQuestionRequest request, Guid? updatedBy = null);
    Task                           PublishAsync(Guid id);
    Task                           UnpublishAsync(Guid id);
    Task                           DeleteAsync(Guid id);
}
