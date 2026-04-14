using GridAcademy.DTOs.Users;

namespace GridAcademy.Services;

public interface IRoleService
{
    Task<List<SystemRoleDto>> GetRolesAsync();
    Task<SystemRoleDto>       GetByIdAsync(int id);
    Task<SystemRoleDto>       CreateAsync(CreateSystemRoleRequest request);
    Task<SystemRoleDto>       UpdateAsync(int id, CreateSystemRoleRequest request);
    Task                      ToggleActiveAsync(int id);
    Task                      DeleteAsync(int id);
}
