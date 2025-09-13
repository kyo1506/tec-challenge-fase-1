using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using TecChallenge.Domain.Interfaces;
using TecChallenge.Infrastructure.Services.Keycloak;
using TecChallenge.Shared.Models.Dtos;
using TecChallenge.Shared.Models.Dtos.Responses;
using TecChallenge.Shared.Models.Generics;

namespace TecChallenge.Infrastructure.Services;

public class KeycloakAdminService(HttpClient httpClient, IOptions<KeycloakAdminConfig> config) : IKeycloakAdminService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly KeycloakAdminConfig _config = config.Value;
    private static string? _adminAccessToken;
    private static DateTime _tokenExpiration;

    public async Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserDto createUserDto)
    {
        await SetAdminTokenAsync();

        var newUser = new KeycloakUserResponse
        {
            Username = createUserDto.Email,
            Email = createUserDto.Email,
            FirstName = createUserDto.FirstName,
            LastName = createUserDto.LastName,
            Enabled = true,
            EmailVerified = true,
            Credentials =
            [
                new()
                {
                    Type = "password",
                    Value = "OAp55CshPZWI4=?o",
                    Temporary = false,
                },
            ],
        };

        var response = await _httpClient.PostAsJsonAsync(_config.UsersEndpointPath, newUser);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return ServiceResult<UserDto>.FailureResult(
                HttpStatusCode.Conflict,
                "User with this email already exists."
            );
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            return ServiceResult<UserDto>.FailureResult(
                HttpStatusCode.BadRequest,
                $"Failed to create user. Details: {errorContent}"
            );
        }

        var locationHeader = response.Headers.Location;
        if (locationHeader == null)
        {
            return ServiceResult<UserDto>.FailureResult(
                HttpStatusCode.InternalServerError,
                "User created but could not retrieve the new User ID."
            );
        }

        var newUserId = locationHeader.Segments.Last();

        if (!string.IsNullOrWhiteSpace(createUserDto.Role))
        {
            var roleAssigned = await AssignRealmRoleToUserAsync(newUserId, createUserDto.Role);
            if (!roleAssigned)
            {
                await DeleteUserAsync(Guid.Parse(newUserId));
                return ServiceResult<UserDto>.FailureResult(
                    HttpStatusCode.InternalServerError,
                    $"User was created, but failed to assign role '{createUserDto.Role}'. The user creation was rolled back."
                );
            }
        }

        var createdUserDto = new UserDto
        {
            Id = Guid.Parse(newUserId),
            Email = createUserDto.Email,
            Username = createUserDto.Email,
            Role = createUserDto.Role,
        };
        return ServiceResult<UserDto>.SuccessResult(createdUserDto, HttpStatusCode.Created);
    }

    private async Task<bool> AssignRealmRoleToUserAsync(string userId, string roleName)
    {
        var roleResponse = await _httpClient.GetAsync(
            $"/admin/realms/{_config.TargetRealm}/roles/{roleName}"
        );
        if (!roleResponse.IsSuccessStatusCode)
            return false;

        var role = await roleResponse.Content.ReadFromJsonAsync<KeycloakRoleResponse>();
        if (role?.Id == null)
            return false;

        var rolesToAssign = new[] { role };
        var assignResponse = await _httpClient.PostAsJsonAsync(
            $"{_config.UsersEndpointPath}/{userId}/role-mappings/realm",
            rolesToAssign
        );

        return assignResponse.IsSuccessStatusCode;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
    {
        var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "client_id", "tech-challenge-api" },
                { "username", loginDto.Email },
                { "password", loginDto.Password },
            }
        );

        var response = await _httpClient.PostAsync(
            $"/realms/{_config.TargetRealm}/protocol/openid-connect/token",
            content
        );

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Keycloak login failed: {errorBody}");
            return null;
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>();

        return new LoginResponseDto { AccessToken = tokenResponse?.AccessToken };
    }

    #region Métodos Existentes
    public async Task<IEnumerable<UserDto>?> GetUsersAsync()
    {
        await SetAdminTokenAsync();
        var response = await _httpClient.GetAsync(_config.UsersEndpointPath);
        if (!response.IsSuccessStatusCode)
            return null;

        var users = await response.Content.ReadFromJsonAsync<List<KeycloakUserResponse>>();
        return users?.Select(u => new UserDto
        {
            Id = Guid.Parse(u.Id!),
            Email = u.Email,
            Username = u.Username,
        });
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        await SetAdminTokenAsync();
        var response = await _httpClient.GetAsync($"{_config.UsersEndpointPath}/{userId}");
        if (!response.IsSuccessStatusCode)
            return null;

        var user = await response.Content.ReadFromJsonAsync<KeycloakUserResponse>();
        return user == null
            ? null
            : new UserDto
            {
                Id = Guid.Parse(user.Id!),
                Email = user.Email,
                Username = user.Username,
                IsDeleted = !user.Enabled,
            };
    }

    public async Task<bool> UpdateUserAsync(Guid userId, UserDto userDto)
    {
        await SetAdminTokenAsync();

        var userRepresentation = new KeycloakUserResponse
        {
            Email = userDto.Email,
            Username = userDto.Username,
            Enabled = !userDto.IsDeleted,
        };

        var response = await _httpClient.PutAsJsonAsync(
            $"{_config.UsersEndpointPath}/{userId}",
            userRepresentation
        );
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        await SetAdminTokenAsync();
        var userRepresentation = new KeycloakUserResponse { Enabled = false };
        var response = await _httpClient.PutAsJsonAsync(
            $"{_config.UsersEndpointPath}/{userId}",
            userRepresentation
        );
        return response.IsSuccessStatusCode;
    }

    private async Task SetAdminTokenAsync()
    {
        if (!string.IsNullOrEmpty(_adminAccessToken) && _tokenExpiration > DateTime.UtcNow)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _adminAccessToken
            );
            return;
        }

        var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", _config.AdminClientId },
                { "client_secret", _config.AdminClientSecret },
            }
        );

        var response = await _httpClient.PostAsync(_config.TokenEndpointPath, content);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>();
        _adminAccessToken = tokenResponse?.AccessToken;
        _tokenExpiration = DateTime.UtcNow.AddSeconds(300);

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _adminAccessToken
        );
    }
    #endregion
}
