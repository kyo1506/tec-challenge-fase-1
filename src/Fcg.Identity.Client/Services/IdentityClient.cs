using System.Net.Http.Json;
using System.Text.Json;
using Fcg.Identity.Client.Interfaces;
using Fcg.Identity.Client.Models;

namespace Fcg.Identity.Client.Services;

/// <summary>
/// Cliente para comunicação com o microserviço de identidade.
/// </summary>
public class IdentityClient : IIdentityClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public IdentityClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task<AuthenticatedUser?> ValidateTokenAsync(string token)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsync("/v1/validate-token", null);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<TokenValidationData>>(
                content,
                _jsonOptions
            );

            if (result?.Success == true && result.Data != null)
            {
                return new AuthenticatedUser
                {
                    UserId = result.Data.UserId,
                    Username = result.Data.Username,
                    Email = result.Data.Email,
                    FirstName = result.Data.FirstName,
                    LastName = result.Data.LastName,
                    Roles = result.Data.Roles,
                    ExpiresAt = result.Data.ExpiresAt,
                };
            }

            return null;
        }
        catch
        {
            return null;
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<bool> ValidatePermissionAsync(string token, string resource, string action)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var requestBody = new { Resource = resource, Action = action };
            var response = await _httpClient.PostAsJsonAsync("/v1/validate-permission", requestBody);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<PermissionValidationData>>(
                content,
                _jsonOptions
            );

            return result?.Success == true && result.Data?.HasPermission == true;
        }
        catch
        {
            return false;
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/v1/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    #region Classes internas para deserialização

    private class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string[]? Errors { get; set; }
    }

    private class TokenValidationData
    {
        public bool IsValid { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public IEnumerable<string> Roles { get; set; } = new List<string>();
        public DateTime? ExpiresAt { get; set; }
    }

    private class PermissionValidationData
    {
        public bool HasPermission { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
    }

    #endregion
}
