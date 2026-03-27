using GridAcademy.Common;
using GridAcademy.DTOs.Users;

namespace GridAcademy.Services;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetUsersAsync(UserListRequest request);
    Task<UserDto> GetByIdAsync(Guid id);
    Task<UserDto> CreateAsync(CreateUserRequest request);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request);
    Task DeleteAsync(Guid id);
}
