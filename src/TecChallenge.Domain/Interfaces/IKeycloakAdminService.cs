using TecChallenge.Shared.Models.Dtos;
using TecChallenge.Shared.Models.Generics;

namespace TecChallenge.Domain.Interfaces;

public interface IKeycloakAdminService
{
    Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
    Task<IEnumerable<UserDto>?> GetUsersAsync();
    Task<UserDto?> GetUserByIdAsync(Guid userId);
    Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserDto createUserDto);
    Task<bool> UpdateUserAsync(Guid userId, UserDto userDto);
    Task<bool> DeleteUserAsync(Guid userId);
}
