using GridAcademy.Common;
using GridAcademy.DTOs.VideoLearning;

namespace GridAcademy.Services.VideoLearning;

public interface IEnrollmentService
{
    Task<PagedResult<EnrollmentDto>>      GetEnrollmentsAsync(EnrollmentListRequest request);
    Task<EnrollmentDto>                   GetByIdAsync(Guid id);
    Task<EnrollmentDto>                   EnrollAsync(CreateEnrollmentRequest request);
    Task                                  CancelAsync(Guid id);
    Task<bool>                            IsEnrolledAsync(Guid userId, Guid programId);
    Task<List<VideoProgressDto>>          GetProgressAsync(Guid enrollmentId);
    Task<VideoProgressDto>                UpsertProgressAsync(Guid enrollmentId, Guid videoId,
                                                               int watchedSeconds, bool markCompleted);
    Task<EnrollmentProgressSummaryDto>    GetProgressSummaryAsync(Guid enrollmentId);
}
