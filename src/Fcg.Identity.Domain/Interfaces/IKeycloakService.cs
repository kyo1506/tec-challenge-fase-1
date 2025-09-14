using Fcg.Identity.Shared.Models.Dtos;
using Fcg.Identity.Shared.Models.Generics;

namespace Fcg.Identity.Domain.Interfaces;

public interface IKeycloakService
{
    Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
    Task<UserDto?> CreateUserAsync(CreateUserDto createUserDto);
    Task<IEnumerable<UserDto>?> GetUsersAsync();
    Task<UserDto?> GetUserByIdAsync(Guid userId);
    Task<bool> UpdateUserAsync(Guid userId, UserDto userDto);
    Task<bool> DeleteUserAsync(Guid userId);
}
