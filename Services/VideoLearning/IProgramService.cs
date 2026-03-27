using GridAcademy.Common;
using GridAcademy.DTOs.VideoLearning;

namespace GridAcademy.Services.VideoLearning;

public interface IProgramService
{
    Task<PagedResult<ProgramSummaryDto>> GetProgramsAsync(ProgramListRequest request);
    Task<ProgramDto>                     GetByIdAsync(Guid id);
    Task<ProgramDto>                     CreateAsync(CreateProgramRequest request, IFormFile? thumbnail = null, Guid? createdBy = null);
    Task<ProgramDto>                     UpdateAsync(Guid id, UpdateProgramRequest request, IFormFile? thumbnail = null, Guid? updatedBy = null);
    Task                                 PublishAsync(Guid id);
    Task                                 UnpublishAsync(Guid id);
    Task                                 DeleteAsync(Guid id);

    Task<PricingPlanDto>                 AddPricingPlanAsync(Guid programId, CreatePricingPlanRequest request);
    Task<PricingPlanDto>                 UpdatePricingPlanAsync(int planId, CreatePricingPlanRequest request);
    Task                                 DeletePricingPlanAsync(int planId);

    Task                                 AddLearningPathAsync(Guid programId, Guid learningPathId, int sortOrder = 0);
    Task                                 RemoveLearningPathAsync(Guid programId, Guid learningPathId);
    Task                                 ReorderLearningPathsAsync(Guid programId, List<ReorderItem> items);

    Task<CourseLaunchDto>                AddCourseLaunchAsync(Guid programId, CreateCourseLaunchRequest request);
    Task<CourseLaunchDto>                UpdateCourseLaunchAsync(int launchId, CreateCourseLaunchRequest request);
    Task                                 DeleteCourseLaunchAsync(int launchId);
    Task<List<CourseLaunchDto>>          GetCourseLaunchesAsync(Guid programId);
}
