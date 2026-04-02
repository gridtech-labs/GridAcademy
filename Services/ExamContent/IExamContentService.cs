using GridAcademy.Data.Entities.Exam;
using GridAcademy.DTOs.ExamContent;

namespace GridAcademy.Services.ExamContent;

public interface IExamContentService
{
    Task<ExamDto> CreateExamAsync(CreateExamRequest request, CancellationToken ct = default);
    Task<PagedResultDto<ExamDto>> GetExamsAsync(string? category, int page, int pageSize, CancellationToken ct = default);
    Task<ExamDto?> GetExamBySlugAsync(string slug, CancellationToken ct = default);

    Task<ExamNotificationDto> CreateNotificationAsync(CreateExamNotificationRequest request, CancellationToken ct = default);
    Task<ExamNotificationDto?> GetNotificationBySlugAsync(string slug, CancellationToken ct = default);
    Task<PagedResultDto<ExamNotificationDto>> GetNotificationsAsync(Guid? examId, string? category, ExamNotificationType? type, int page, int pageSize, CancellationToken ct = default);

    Task<ExamNotificationDto> UpdateNotificationAsync(Guid id, UpdateExamNotificationRequest request, CancellationToken ct = default);
    Task<ExamNotificationDto> ChangeStatusAsync(Guid id, PublicationStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<ContentVersionDto>> GetVersionHistoryAsync(Guid notificationId, CancellationToken ct = default);
}
