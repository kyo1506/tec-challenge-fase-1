using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Fcg.Identity.Domain.Interfaces;
using Fcg.Identity.Domain.Notifications;
using Fcg.Identity.Infrastructure.Extensions;
using Fcg.Identity.Shared.Models.Dtos;
using Fcg.Identity.Shared.Models.Dtos.Responses;
using Microsoft.Extensions.Options;

namespace Fcg.Identity.Infrastructure.Services;

public class KeycloakService(
    HttpClient httpClient,
    IOptions<KeycloakConfiguration> config,
    INotifier notifier
) : IKeycloakService
{
    private readonly KeycloakConfiguration _config = config.Value;
    private static string? _adminAccessToken;
    private static DateTime _tokenExpiration;

    public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
    {
        var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "client_id", _config.AdminClientId },
                { "username", loginDto.Email },
                { "password", loginDto.Password },
            }
        );

        var response = await httpClient.PostAsync(_config.TokenEndpointPath, content);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>();
        return new LoginResponseDto { AccessToken = tokenResponse?.AccessToken };
    }

    public async Task<UserDto?> CreateUserAsync(CreateUserDto createUserDto)
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
                    Value = "Default@123",
                    Temporary = true,
                },
            ],
        };

        var response = await httpClient.PostAsJsonAsync(_config.UsersEndpointPath, newUser);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            notifier.Handle(new Notification("Já existe um usuário com este e-mail."));
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            notifier.Handle(
                new Notification($"Falha ao criar usuário no Keycloak. Detalhes: {errorContent}")
            );
            return null;
        }

        var locationHeader = response.Headers.Location;
        if (locationHeader == null)
        {
            notifier.Handle(
                new Notification("Usuário criado, mas não foi possível obter o novo ID do usuário.")
            );
            return null;
        }

        var newUserId = locationHeader.Segments.Last();

        if (!string.IsNullOrWhiteSpace(createUserDto.Role))
        {
            var roleAssigned = await AssignRealmRoleToUserAsync(newUserId, createUserDto.Role);
            if (!roleAssigned)
            {
                await DeleteUserAsync(Guid.Parse(newUserId)); // Rollback da criação
                notifier.Handle(
                    new Notification(
                        $"Usuário criado, mas falha ao atribuir o papel '{createUserDto.Role}'. A criação foi desfeita."
                    )
                );
                return null;
            }
        }

        return new UserDto
        {
            Id = Guid.Parse(newUserId),
            Email = createUserDto.Email,
            Username = createUserDto.Email,
            Role = createUserDto.Role,
        };
    }

    public async Task<IEnumerable<UserDto>?> GetUsersAsync()
    {
        await SetAdminTokenAsync();
        var response = await httpClient.GetAsync(_config.UsersEndpointPath);
        if (!response.IsSuccessStatusCode)
        {
            notifier.Handle(new Notification("Não foi possível buscar a lista de usuários."));
            return null;
        }

        var users = await response.Content.ReadFromJsonAsync<List<KeycloakUserResponse>>();
        return users?.Select(u => new UserDto
        {
            Id = Guid.Parse(u.Id!),
            Email = u.Email,
            Username = u.Username,
            IsDeleted = !u.Enabled,
        });
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        await SetAdminTokenAsync();
        var response = await httpClient.GetAsync($"{_config.UsersEndpointPath}/{userId}");
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
        var userRepresentation = new
        {
            email = userDto.Email,
            username = userDto.Username,
            enabled = !userDto.IsDeleted,
        };

        var response = await httpClient.PutAsJsonAsync(
            $"{_config.UsersEndpointPath}/{userId}",
            userRepresentation
        );

        if (!response.IsSuccessStatusCode)
        {
            notifier.Handle(new Notification("Falha ao atualizar o usuário."));
            return false;
        }
        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        await SetAdminTokenAsync();
        var userRepresentation = new { enabled = false };
        var response = await httpClient.PutAsJsonAsync(
            $"{_config.UsersEndpointPath}/{userId}",
            userRepresentation
        );

        if (!response.IsSuccessStatusCode)
        {
            notifier.Handle(new Notification("Falha ao desabilitar o usuário."));
            return false;
        }
        return true;
    }

    #region Private Methods

    private async Task<bool> AssignRealmRoleToUserAsync(string userId, string roleName)
    {
        var roleResponse = await httpClient.GetAsync(
            $"/admin/realms/{_config.TargetRealm}/roles/{roleName}"
        );
        if (!roleResponse.IsSuccessStatusCode)
            return false;

        var role = await roleResponse.Content.ReadFromJsonAsync<KeycloakRoleResponse>();
        if (role?.Id == null)
            return false;

        var rolesToAssign = new[] { role };
        var assignResponse = await httpClient.PostAsJsonAsync(
            $"{_config.UsersEndpointPath}/{userId}/role-mappings/realm",
            rolesToAssign
        );

        return assignResponse.IsSuccessStatusCode;
    }

    private async Task SetAdminTokenAsync()
    {
        if (!string.IsNullOrEmpty(_adminAccessToken) && _tokenExpiration > DateTime.UtcNow)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
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

        var response = await httpClient.PostAsync(_config.TokenEndpointPath, content);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>();
        _adminAccessToken = tokenResponse?.AccessToken;
        _tokenExpiration = DateTime.UtcNow.AddSeconds(300); // 5 minutos de validade

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _adminAccessToken
        );
    }
    #endregion
}
