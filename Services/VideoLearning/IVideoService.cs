using GridAcademy.Common;
using GridAcademy.DTOs.VideoLearning;
using Microsoft.AspNetCore.Http;

namespace GridAcademy.Services.VideoLearning;

public interface IVideoService
{
    Task<PagedResult<VideoDto>>  GetVideosAsync(VideoListRequest request);
    Task<VideoDto>               GetByIdAsync(Guid id);
    Task<VideoDto>               CreateAsync(CreateVideoRequest request, IFormFile? videoFile,
                                             IFormFile? thumbnail, Guid? createdBy = null);
    Task<VideoDto>               UpdateAsync(Guid id, UpdateVideoRequest request,
                                             IFormFile? thumbnail, Guid? updatedBy = null);
    Task                         ReplaceVideoFileAsync(Guid id, IFormFile videoFile);
    Task                         PublishAsync(Guid id);
    Task                         UnpublishAsync(Guid id);
    Task                         DeleteAsync(Guid id);
}
