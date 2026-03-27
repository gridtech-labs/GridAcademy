using GridAcademy.DTOs.VideoLearning;

namespace GridAcademy.Services.VideoLearning;

public interface IVideoCategoryService
{
    Task<List<VideoCategoryDto>> GetByDomainAsync(int domainId, bool activeOnly = true);
    Task<List<VideoCategoryDto>> GetAllAsync(bool activeOnly = true);
    Task<VideoCategoryDto>       GetByIdAsync(int id);
    Task<VideoCategoryDto>       CreateAsync(CreateVideoCategoryRequest request);
    Task<VideoCategoryDto>       UpdateAsync(int id, CreateVideoCategoryRequest request);
    Task                         DeleteAsync(int id);
}
