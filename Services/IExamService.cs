using GridAcademy.DTOs.Exam;
using GridAcademy.Data.Entities.Exam;

namespace GridAcademy.Services;

public interface IExamService
{
    // Exam Levels
    Task<List<ExamLevelDto>>     GetExamLevelsAsync();
    Task<ExamLevelDto>           SaveExamLevelAsync(int? id, SaveExamLevelRequest request);
    Task                         DeleteExamLevelAsync(int id);

    // Exam Pages
    Task<List<ExamPageCardDto>>  GetExamPagesAsync(bool activeOnly = false, int? levelId = null, string? search = null);
    Task<ExamPageDetailDto?>     GetExamBySlugAsync(string slug, bool incrementView = false);
    Task<ExamPageDetailDto?>     GetExamByIdAsync(Guid id);
    Task<ExamPageCardDto>        CreateExamAsync(SaveExamPageRequest request, Guid? createdBy = null);
    Task<ExamPageCardDto>        UpdateExamAsync(Guid id, SaveExamPageRequest request, Guid? updatedBy = null);
    Task                         DeleteExamAsync(Guid id);

    // Test mapping
    Task                         MapTestAsync(Guid examId, MapTestRequest request);
    Task                         UnmapTestAsync(Guid examId, Guid testId);
    Task<List<ExamTestDto>>      GetMappedTestsAsync(Guid examId);
}
