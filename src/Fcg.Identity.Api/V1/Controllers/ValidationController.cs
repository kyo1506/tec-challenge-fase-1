using System.Net;
using Fcg.Identity.Shared.Constants;
using Fcg.Identity.Shared.Models.Requests;
using Fcg.Identity.Shared.Models.Responses;
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
        Console.WriteLine("[TRACE] ValidateToken endpoint called");
        Console.WriteLine($"[TRACE] Request method: {Request.Method}");
        Console.WriteLine($"[TRACE] Request path: {Request.Path}");
        Console.WriteLine($"[TRACE] Content-Type: {Request.ContentType}");
        Console.WriteLine($"[TRACE] User-Agent: {Request.Headers.UserAgent}");

        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        Console.WriteLine($"[TRACE] Authorization header present: {authHeader != null}");
        Console.WriteLine(
            $"[TRACE] Authorization header value: {authHeader?[..Math.Min(30, authHeader?.Length ?? 0)]}..."
        );

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            Console.WriteLine("[ERROR] Token not provided or invalid format");
            NotifyError("Token not provided or invalid format.");
            return CustomResponse<TokenValidationResponse>(statusCode: HttpStatusCode.Unauthorized);
        }

        var token = authHeader.Replace("Bearer ", "");
        Console.WriteLine($"[TRACE] Token extracted, length: {token.Length}");

        try
        {
            // Log the token for debugging (first 20 chars only for security)
            Console.WriteLine(
                $"[DEBUG] Validating token starting with: {token[..Math.Min(20, token.Length)]}..."
            );

            Console.WriteLine("[TRACE] Calling keycloakService.ValidateTokenAsync");
            // Validate token via Keycloak
            var userInfo = await keycloakService.ValidateTokenAsync(token);
            Console.WriteLine(
                $"[TRACE] keycloakService.ValidateTokenAsync returned: {userInfo != null}"
            );

            if (userInfo == null)
            {
                Console.WriteLine(
                    "[ERROR] Keycloak returned null userInfo - token invalid or expired"
                );
                NotifyError("Invalid or expired token.");
                return CustomResponse<TokenValidationResponse>(
                    statusCode: HttpStatusCode.Unauthorized
                );
            }

            Console.WriteLine(
                $"[TRACE] User validated successfully. UserId: {userInfo.Id}, Username: {userInfo.Username}"
            );
            Console.WriteLine($"[TRACE] User roles: {string.Join(", ", userInfo.Roles ?? [])}");

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

            Console.WriteLine("[TRACE] ValidateToken returning success response");
            return CustomResponse(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Exception in ValidateToken: {ex.Message}");
            Console.WriteLine($"[ERROR] Exception type: {ex.GetType().Name}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[ERROR] Inner exception: {ex.InnerException.Message}");
            }
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
        Console.WriteLine("[TRACE] ValidatePermission endpoint called");
        Console.WriteLine($"[TRACE] Request method: {Request.Method}");
        Console.WriteLine($"[TRACE] Request path: {Request.Path}");

        if (request == null)
        {
            Console.WriteLine("[ERROR] ValidatePermission request is null");
            NotifyError("Request cannot be null.");
            return CustomResponse<PermissionValidationResponse>(
                statusCode: HttpStatusCode.BadRequest
            );
        }

        Console.WriteLine(
            $"[TRACE] Permission request - Resource: {request.Resource}, Action: {request.Action}"
        );

        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        Console.WriteLine($"[TRACE] Authorization header present: {authHeader != null}");

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            Console.WriteLine("[ERROR] ValidatePermission - Token not provided or invalid format");
            NotifyError("Token not provided or invalid format.");
            return CustomResponse<PermissionValidationResponse>(
                statusCode: HttpStatusCode.Unauthorized
            );
        }

        var token = authHeader.Replace("Bearer ", "");
        Console.WriteLine($"[TRACE] ValidatePermission - Token extracted, length: {token.Length}");

        try
        {
            Console.WriteLine(
                "[TRACE] ValidatePermission - Calling keycloakService.ValidateTokenAsync"
            );
            // Validate token first
            var userInfo = await keycloakService.ValidateTokenAsync(token);
            Console.WriteLine(
                $"[TRACE] ValidatePermission - Token validation result: {userInfo != null}"
            );

            if (userInfo == null)
            {
                Console.WriteLine("[ERROR] ValidatePermission - Keycloak returned null userInfo");
                NotifyError("Invalid or expired token.");
                return CustomResponse<PermissionValidationResponse>(
                    statusCode: HttpStatusCode.Unauthorized
                );
            }

            Console.WriteLine(
                $"[TRACE] ValidatePermission - User validated: {userInfo.Username} ({userInfo.Id})"
            );
            Console.WriteLine(
                $"[TRACE] ValidatePermission - User roles: {string.Join(", ", userInfo.Roles ?? [])}"
            );

            Console.WriteLine(
                $"[TRACE] ValidatePermission - Checking permission {request.Resource}:{request.Action}"
            );
            // Check user permissions
            var hasPermission = await ValidateUserPermission(
                userInfo,
                request.Resource,
                request.Action
            );

            Console.WriteLine(
                $"[TRACE] ValidatePermission - Permission check result: {hasPermission}"
            );

            var response = new PermissionValidationResponse
            {
                HasPermission = hasPermission,
                UserId = userInfo.Id,
                Resource = request.Resource,
                Action = request.Action,
            };

            Console.WriteLine("[TRACE] ValidatePermission returning response");
            return CustomResponse(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Exception in ValidatePermission: {ex.Message}");
            Console.WriteLine($"[ERROR] Exception type: {ex.GetType().Name}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[ERROR] Inner exception: {ex.InnerException.Message}");
            }
            NotifyError("Error validating permission.");
            return CustomResponse<PermissionValidationResponse>(
                statusCode: HttpStatusCode.Unauthorized
            );
        }
    }

    #region Private Methods

    /// <summary>
    /// Valida se um usuário tem permissão para executar uma ação específica em um recurso.
    /// Sistema simplificado com apenas 3 permissões: users:manage, users:read e profiles:manage
    /// </summary>
    /// <param name="userInfo">Informações do usuário incluindo roles</param>
    /// <param name="resource">Recurso alvo (ex: "users", "profile")</param>
    /// <param name="action">Ação desejada (ex: "read", "manage")</param>
    /// <returns>True se o usuário tem a permissão, False caso contrário</returns>
    private static Task<bool> ValidateUserPermission(
        UserResponse userInfo,
        string resource,
        string action
    )
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
            ["admin"] =
            [
                Permissions.Users.Read,
                Permissions.Users.Manage,
                Permissions.Profile.Manage,
            ],
            ["user"] = [Permissions.Profile.Manage],
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
