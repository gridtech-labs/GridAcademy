using GridAcademy.Data.Entities.Exam;
using GridAcademy.DTOs.ExamContent;

namespace GridAcademy.Services.ExamContent;

public interface IContentWorkflowService
{
    Task<ContentActionResponseDto> ApproveAsync(Guid id, CancellationToken ct = default);
    Task<ContentActionResponseDto> PublishAsync(Guid id, CancellationToken ct = default);
    Task<PublicContentDto?> GetPublishedBySlugAsync(string slug, CancellationToken ct = default);
    Task<AdminContentListResponseDto> GetAdminContentAsync(PublicationStatus? status, int page, int pageSize, CancellationToken ct = default);
}
