using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Fcg.Identity.Domain.Interfaces;
using Fcg.Identity.Domain.Notifications;
using Fcg.Identity.Infrastructure.Extensions;
using Fcg.Identity.Shared.Mappers;
using Fcg.Identity.Shared.Models.Requests;
using Fcg.Identity.Shared.Models.Responses;
using Microsoft.Extensions.Options;

namespace Fcg.Identity.Infrastructure.Services;

public class KeycloakService(
    HttpClient httpClient,
    IOptions<KeycloakConfiguration> config,
    INotifier notifier,
    IUser userAuthenticated
) : IKeycloakService
{
    private readonly KeycloakConfiguration _config = config.Value;
    private static string? _adminAccessToken;
    private static DateTime _tokenExpiration;

    public async Task<TokenResponse?> LoginAsync(LoginRequest request)
    {
        var requestData = new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "client_id", _config.ApiClientId },
            { "password", request.Password },
            { "client_secret", _config.ApiClientSecret },
            { "scope", "openid profile email" },
        };

        string userIdentifier = !string.IsNullOrEmpty(request.Username)
            ? request.Username
            : request.Email!;

        requestData.Add("username", userIdentifier);

        var content = new FormUrlEncodedContent(requestData);
        var tokenEndpoint = $"/realms/{_config.TargetRealm}/protocol/openid-connect/token";
        var response = await httpClient.PostAsync(tokenEndpoint, content);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var keycloakTokenResponse =
            await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>();

        return keycloakTokenResponse?.ToTokenResponse();
    }

    public async Task<UserResponse?> CreateUserAsync(CreateUserRequest request)
    {
        return await ExecuteWithTokenAsync(async () =>
        {
            var newUser = new KeycloakUserRequest
            {
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Enabled = true,
                EmailVerified = true,
                RequiredActions = ["UPDATE_PASSWORD"],
            };

            var response = await httpClient.PostAsJsonAsync(_config.UsersEndpointPath, newUser);

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                notifier.Handle(
                    new Notification("A user with this email or username already exists.")
                );
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                notifier.Handle(
                    new Notification($"Failed to create user in Keycloak. Details: {errorContent}")
                );
                return null;
            }

            var locationHeader = response.Headers.Location;
            if (locationHeader == null)
            {
                notifier.Handle(
                    new Notification("User created, but could not retrieve the new User ID.")
                );
                return null;
            }

            var newUserId = locationHeader.Segments.Last();
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var roleAssigned = await AssignRealmRoleToUserAsync(newUserId, [request.Role]);
                if (!roleAssigned)
                {
                    await DeleteUserAsync(Guid.Parse(newUserId)); // Rollback
                    notifier.Handle(
                        new Notification(
                            $"User created, but failed to assign role '{request.Role}'. The creation was rolled back."
                        )
                    );
                    return null;
                }
            }

            return new UserResponse
            {
                Id = Guid.Parse(newUserId),
                Email = request.Email,
                Username = request.Username,
                Role = request.Role,
                IsEnabled = newUser.Enabled,
            };
        });
    }

    public async Task<IEnumerable<UserResponse>?> GetUsersAsync()
    {
        return await ExecuteWithTokenAsync(async () =>
        {
            var response = await httpClient.GetAsync(_config.UsersEndpointPath);

            if (!response.IsSuccessStatusCode)
            {
                notifier.Handle(new Notification("Could not fetch the list of users."));
                return null;
            }

            var usersResponse = await response.Content.ReadFromJsonAsync<
                List<KeycloakUserResponse>
            >();
            if (usersResponse == null)
                return Enumerable.Empty<UserResponse>();

            // PERFORMANCE NOTE: This loop creates N+1 requests (1 for users, N for roles).
            // This is acceptable for a small number of users but can be slow for large user bases.
            var userTasks = usersResponse.Select(async user =>
            {
                var roles = await GetUserRolesAsync(user.Id);
                return new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.Username,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsEnabled = user.Enabled,
                    Role = roles?.FirstOrDefault(role =>
                        !role.StartsWith("default-roles-", StringComparison.OrdinalIgnoreCase)
                    ),
                    Roles = roles ?? [],
                };
            });

            return (IEnumerable<UserResponse>)await Task.WhenAll(userTasks);
        });
    }

    public async Task<UserResponse?> GetUserByIdAsync(Guid userId)
    {
        return await ExecuteWithTokenAsync(async () =>
        {
            var response = await httpClient.GetAsync($"{_config.UsersEndpointPath}/{userId}");
            if (!response.IsSuccessStatusCode)
                return null;

            var user = await response.Content.ReadFromJsonAsync<KeycloakUserResponse>();
            if (user?.Id == null)
                return null;

            var roles = await GetUserRolesAsync(user.Id);

            return new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsEnabled = user.Enabled,
                Role = roles?.FirstOrDefault(role =>
                    !role.StartsWith("default-roles-", StringComparison.OrdinalIgnoreCase)
                ),
                Roles = roles ?? [],
            };
        });
    }

    public async Task<UpdateUserResponse?> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        return await ExecuteWithTokenAsync(async () =>
        {
            var userRepresentation = new
            {
                email = request.Email,
                firstName = request.FirstName,
                lastName = request.LastName,
                enabled = request.IsEnabled,
            };

            var response = await httpClient.PutAsJsonAsync(
                $"{_config.UsersEndpointPath}/{userId}",
                userRepresentation
            );

            if (!response.IsSuccessStatusCode)
            {
                notifier.Handle(new Notification("Failed to update user details."));
                return null;
            }

            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var currentRoles = await GetUserRolesAsync(userId);
                var currentRole = currentRoles?.FirstOrDefault(role =>
                    !role.StartsWith("default-roles-", StringComparison.OrdinalIgnoreCase)
                );

                if (currentRole != request.Role)
                {
                    if (currentRole != null)
                    {
                        await RemoveRealmRoleFromUserAsync(userId, [currentRole]);
                    }

                    var assigned = await AssignRealmRoleToUserAsync(
                        userId.ToString(),
                        [request.Role]
                    );
                    if (!assigned)
                    {
                        notifier.Handle(
                            new Notification(
                                $"User details updated, but failed to assign new role '{request.Role}'."
                            )
                        );
                        return null;
                    }
                }
            }

            var authenticatedUserId = userAuthenticated.GetUserId();

            return new UpdateUserResponse
            {
                Message =
                    authenticatedUserId == userId
                        ? "User updated successfully. Please re-login to refresh your session."
                        : "User updated successfully.",
                ActionRequired = authenticatedUserId == userId ? "reauthenticate" : null,
            };
        });
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        return await ExecuteWithTokenAsync(async () =>
        {
            var userRepresentation = new { enabled = false };
            var response = await httpClient.PutAsJsonAsync(
                $"{_config.UsersEndpointPath}/{userId}",
                userRepresentation
            );
            if (!response.IsSuccessStatusCode)
            {
                notifier.Handle(new Notification("Failed to disable user."));
                return false;
            }
            return true;
        });
    }

    #region Private Methods

    private async Task<List<string>?> GetUserRolesAsync(Guid userId)
    {
        // This method will already be executed within the ExecuteWithTokenAsync context
        // so there's no need to call SetAdminTokenAsync again
        var roleMappingsUrl = $"{_config.UsersEndpointPath}/{userId}/role-mappings/realm";
        var rolesResponse = await httpClient.GetAsync(roleMappingsUrl);
        if (!rolesResponse.IsSuccessStatusCode)
            return null;

        var roles = await rolesResponse.Content.ReadFromJsonAsync<List<KeycloakRoleResponse>>();
        return roles?.Select(r => r.Name).Where(n => n != null).ToList()!;
    }

    private async Task<bool> AssignRealmRoleToUserAsync(string userId, List<string> roleNames)
    {
        // Este método já será executado dentro do contexto do ExecuteWithTokenAsync
        var allRealmRoles = await GetAllRealmRoles();
        var rolesToAssign = allRealmRoles.Where(r => roleNames.Contains(r.Name)).ToList();

        if (rolesToAssign.Count != roleNames.Count)
            return false;

        var assignResponse = await httpClient.PostAsJsonAsync(
            $"{_config.UsersEndpointPath}/{userId}/role-mappings/realm",
            rolesToAssign
        );
        return assignResponse.IsSuccessStatusCode;
    }

    private async Task<bool> RemoveRealmRoleFromUserAsync(Guid userId, List<string> roleNames)
    {
        // Este método já será executado dentro do contexto do ExecuteWithTokenAsync
        var allRealmRoles = await GetAllRealmRoles();
        var rolesToRemove = allRealmRoles.Where(r => roleNames.Contains(r.Name)).ToList();

        if (rolesToRemove.Count != roleNames.Count)
            return false;

        var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{_config.UsersEndpointPath}/{userId}/role-mappings/realm"
        )
        {
            Content = JsonContent.Create(rolesToRemove),
        };
        var removeResponse = await httpClient.SendAsync(request);
        return removeResponse.IsSuccessStatusCode;
    }

    private async Task<List<KeycloakRoleResponse>> GetAllRealmRoles()
    {
        // Este método já será executado dentro do contexto do ExecuteWithTokenAsync
        var rolesUrl = $"/admin/realms/{_config.TargetRealm}/roles";
        var response = await httpClient.GetAsync(rolesUrl);
        if (!response.IsSuccessStatusCode)
            return [];

        return await response.Content.ReadFromJsonAsync<List<KeycloakRoleResponse>>() ?? [];
    }

    private async Task SetAdminTokenAsync()
    {
        // Verifica se o token ainda é válido (com margem de segurança de 60 segundos)
        if (
            !string.IsNullOrEmpty(_adminAccessToken)
            && _tokenExpiration > DateTime.UtcNow.AddSeconds(60)
        )
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _adminAccessToken
            );
            return;
        }

        // ✅ RETRY LOGIC MELHORADO
        var maxRetries = 3;
        var baseDelay = TimeSpan.FromSeconds(2);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
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

                var tokenResponse =
                    await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>();

                if (tokenResponse?.AccessToken == null)
                {
                    throw new InvalidOperationException(
                        "Token response is null or access token is missing"
                    );
                }

                _adminAccessToken = tokenResponse.AccessToken;

                // Use the real token expiration time, with safety margin
                var expiresInSeconds = tokenResponse.ExpiresIn > 0 ? tokenResponse.ExpiresIn : 3600;
                _tokenExpiration = DateTime.UtcNow.AddSeconds(expiresInSeconds - 60);

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    _adminAccessToken
                );

                return;
            }
            catch (HttpRequestException ex)
                when (attempt < maxRetries
                    && (
                        ex.Message.Contains("Connection refused")
                        || ex.Message.Contains("timeout")
                        || ex.Message.Contains("No such host")
                    )
                )
            {
                var delay = TimeSpan.FromMilliseconds(
                    baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1)
                );
                await Task.Delay(delay);
            }
            catch (Exception)
            {
                if (attempt == maxRetries)
                {
                    _adminAccessToken = null;
                    _tokenExpiration = DateTime.MinValue;
                    httpClient.DefaultRequestHeaders.Authorization = null;
                    throw;
                }
            }
        }
    }

    private async Task<T> ExecuteWithTokenAsync<T>(Func<Task<T>> operation)
    {
        await SetAdminTokenAsync();

        try
        {
            return await operation();
        }
        catch (HttpRequestException ex)
            when (ex.Message.Contains("401") || ex.Message.Contains("Unauthorized"))
        {
            // Token may have expired, force renewal
            _adminAccessToken = null;
            _tokenExpiration = DateTime.MinValue;

            // Tenta novamente com novo token
            await SetAdminTokenAsync();
            return await operation();
        }
    }

    public async Task<UserResponse?> ValidateTokenAsync(string token)
    {
        try
        {
            // Configurar o token no header da requisição
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                token
            );

            // Make request to Keycloak user info endpoint using GET method as per OpenID Connect spec
            var userInfoEndpoint =
                $"/realms/{_config.TargetRealm}/protocol/openid-connect/userinfo";

            var response = await httpClient.GetAsync(userInfoEndpoint);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var userInfoResponse =
                await response.Content.ReadFromJsonAsync<KeycloakUserInfoResponse>();
            if (userInfoResponse == null)
            {
                return null;
            }

            // Search for complete user information using the ID obtained from the token
            var userId = Guid.Parse(userInfoResponse.Sub);
            var fullUserInfo = await GetUserByIdAsync(userId);

            if (fullUserInfo != null)
            {
                // Add token expiration information if available
                fullUserInfo.ExpiresAt = ExtractTokenExpiration(token);
            }

            return fullUserInfo;
        }
        catch (Exception)
        {
            // In case of error, consider token invalid
            return null;
        }
        finally
        {
            // Remove token from header to not affect other requests
            httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    private static DateTime? ExtractTokenExpiration(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
                return null;

            var payload = parts[1];

            switch (payload.Length % 4)
            {
                case 2:
                    payload += "==";
                    break;
                case 3:
                    payload += "=";
                    break;
            }

            var jsonBytes = Convert.FromBase64String(payload);
            var jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);
            var tokenInfo = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                jsonString
            );

            if (tokenInfo != null && tokenInfo.ContainsKey("exp"))
            {
                var expTimestamp = Convert.ToInt64(tokenInfo["exp"].ToString());
                return DateTimeOffset.FromUnixTimeSeconds(expTimestamp).DateTime;
            }
        }
        catch { }

        return null;
    }

    #endregion

    #region Auxiliary classes for token validation

    private class KeycloakUserInfoResponse
    {
        public string Sub { get; set; } = string.Empty;
        public string PreferredUsername { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string GivenName { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
    }

    #endregion
}
