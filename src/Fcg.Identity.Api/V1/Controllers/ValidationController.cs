using System.Net;
using Fcg.Identity.Shared.Constants;
using Fcg.Identity.Shared.Models.Requests;
using Fcg.Identity.Shared.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fcg.Identity.Api.V1.Controllers;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}")]
[ApiController]
public class ValidationController(
    INotifier notifier,
    IUser appUser,
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment webHostEnvironment,
    IKeycloakService keycloakService
) : MainController(notifier, appUser, httpContextAccessor, webHostEnvironment)
{
    /// <summary>
    /// Validates a JWT token for other microservices.
    /// </summary>
    /// <remarks>
    /// This endpoint is used internally by other microservices to validate tokens.
    /// Returns user information if the token is valid.
    /// </remarks>
    [HttpPost("validate-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Root<TokenValidationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Root<TokenValidationResponse>>> ValidateToken()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            NotifyError("Token not provided or invalid format.");
            return CustomResponse<TokenValidationResponse>(statusCode: HttpStatusCode.Unauthorized);
        }

        var token = authHeader.Replace("Bearer ", "");

        try
        {
            // Log the token for debugging (first 20 chars only for security)
            Console.WriteLine(
                $"[DEBUG] Validating token starting with: {token[..Math.Min(20, token.Length)]}..."
            );

            // Validate token via Keycloak
            var userInfo = await keycloakService.ValidateTokenAsync(token);

            if (userInfo == null)
            {
                NotifyError("Invalid or expired token.");
                return CustomResponse<TokenValidationResponse>(
                    statusCode: HttpStatusCode.Unauthorized
                );
            }

            var response = new TokenValidationResponse
            {
                IsValid = true,
                UserId = userInfo.Id,
                Username = userInfo.Username,
                Email = userInfo.Email,
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                ExpiresAt = userInfo.ExpiresAt,
                Roles = userInfo.Roles,
            };

            return CustomResponse(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Exception in ValidateToken: {ex.Message}");
            Console.WriteLine($"[DEBUG] Stack trace: {ex.StackTrace}");
            NotifyError("Error validating token.");
            return CustomResponse<TokenValidationResponse>(statusCode: HttpStatusCode.Unauthorized);
        }
    }

    /// <summary>
    /// Validates a user's permission for a specific resource and action.
    /// </summary>
    /// <remarks>
    /// This endpoint is used by other microservices to check if a user has the required permissions
    /// for accessing specific resources or performing certain actions.
    /// </remarks>
    [HttpPost("validate-permission")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Root<PermissionValidationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Root<PermissionValidationResponse>>> ValidatePermission(
        [FromBody] PermissionValidationRequest request
    )
    {
        if (request == null)
        {
            NotifyError("Request cannot be null.");
            return CustomResponse<PermissionValidationResponse>(
                statusCode: HttpStatusCode.BadRequest
            );
        }

        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            NotifyError("Token not provided or invalid format.");
            return CustomResponse<PermissionValidationResponse>(
                statusCode: HttpStatusCode.Unauthorized
            );
        }

        var token = authHeader.Replace("Bearer ", "");

        try
        {
            // Validate token first
            var userInfo = await keycloakService.ValidateTokenAsync(token);

            if (userInfo == null)
            {
                NotifyError("Invalid or expired token.");
                return CustomResponse<PermissionValidationResponse>(
                    statusCode: HttpStatusCode.Unauthorized
                );
            }

            // Check user permissions
            var hasPermission = await ValidateUserPermission(
                userInfo,
                request.Resource,
                request.Action
            );

            var response = new PermissionValidationResponse
            {
                HasPermission = hasPermission,
                UserId = userInfo.Id,
                Resource = request.Resource,
                Action = request.Action,
            };

            return CustomResponse(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Exception in ValidatePermission: {ex.Message}");
            NotifyError("Error validating permission.");
            return CustomResponse<PermissionValidationResponse>(
                statusCode: HttpStatusCode.Unauthorized
            );
        }
    }

    #region Private Methods

    /// <summary>
    /// Valida se um usuário tem permissão para executar uma ação específica em um recurso.
    /// Sistema simplificado com apenas 3 permissões: users:manage, users:read e profile:manage
    /// </summary>
    /// <param name="userInfo">Informações do usuário incluindo roles</param>
    /// <param name="resource">Recurso alvo (ex: "users", "profile")</param>
    /// <param name="action">Ação desejada (ex: "read", "manage")</param>
    /// <returns>True se o usuário tem a permissão, False caso contrário</returns>
    private Task<bool> ValidateUserPermission(UserResponse userInfo, string resource, string action)
    {
        // Obtém as roles do usuário vindas do Keycloak
        var userRoles = userInfo.Roles ?? [];
        
        // Log para debug
        Console.WriteLine(
            $"[DEBUG] Validating permission {resource}:{action} for user roles: {string.Join(", ", userRoles)}"
        );

        // Matriz de permissões por role - Sistema simplificado com apenas 3 permissões
        var rolePermissions = new Dictionary<string, List<string>>
        {
            ["admin"] = [Permissions.Users.Read, Permissions.Users.Manage, Permissions.Profile.Manage],
            ["manager"] = [Permissions.Users.Read, Permissions.Profile.Manage],
            ["user"] = [Permissions.Profile.Manage],
            ["customer"] = [Permissions.Profile.Manage],
        };

        // Check for exact permission match
        var requiredPermission = $"{resource}:{action}";

        foreach (var role in userRoles)
        {
            var normalizedRole = role.ToLowerInvariant().Replace("role_", "");

            if (rolePermissions.ContainsKey(normalizedRole))
            {
                if (rolePermissions[normalizedRole].Contains(requiredPermission))
                {
                    return Task.FromResult(true);
                }
            }
        }

        return Task.FromResult(false);
    }

    #endregion
}
