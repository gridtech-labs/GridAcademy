using GridAcademy.Common;
using GridAcademy.DTOs.VideoLearning;
using Microsoft.AspNetCore.Http;

namespace GridAcademy.Services.VideoLearning;

public interface IContentFileService
{
    Task<PagedResult<ContentFileDto>>  GetFilesAsync(ContentFileListRequest request);
    Task<ContentFileDto>               GetByIdAsync(Guid id);
    Task<ContentFileDto>               CreateAsync(CreateContentFileRequest request, IFormFile? file, Guid? createdBy = null);
    Task<ContentFileDto>               UpdateAsync(Guid id, CreateContentFileRequest request, IFormFile? file = null, Guid? updatedBy = null);
    Task                               DeleteAsync(Guid id);
}
