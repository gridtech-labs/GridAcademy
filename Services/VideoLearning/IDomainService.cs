using GridAcademy.DTOs.VideoLearning;

namespace GridAcademy.Services.VideoLearning;

public interface IDomainService
{
    Task<List<DomainDto>>  GetAllAsync(bool activeOnly = true);
    Task<DomainDto>        GetByIdAsync(int id);
    Task<DomainDto>        CreateAsync(CreateDomainRequest request);
    Task<DomainDto>        UpdateAsync(int id, CreateDomainRequest request);
    Task                   DeleteAsync(int id);
}
