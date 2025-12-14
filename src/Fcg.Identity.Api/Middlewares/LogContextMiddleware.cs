using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Serilog.Context;

namespace Fcg.Identity.Api.Middlewares;

/// <summary>
/// Middleware que combina funcionalidades:
/// 1. Enriquecimento de logs com contexto de requisição
/// 2. Extração de informações do JWT (validação feita pelo Kong)
/// 3. Suporte completo ao Kong Ingress Controller
/// </summary>
public class LogContextMiddleware(RequestDelegate next, ILogger<LogContextMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<LogContextMiddleware> _logger = logger;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public async Task InvokeAsync(HttpContext context)
    {
        // RequestId gerado pelo Kong
        var requestId =
            context.Request.Headers["X-Kong-Request-ID"].FirstOrDefault() ?? Guid.NewGuid()
                .ToString("N")[..8];

        // CorrelationId vindo do Kong ou do cliente
        var correlationId =
            context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid()
                .ToString("N")[..12];

        // Garante que o correlationId seja devolvido ao client
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        var userInfo = ExtractUserInfo(context.Request);

        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("SessionId", userInfo?.SessionId ?? ""))
        using (LogContext.PushProperty("UserId", userInfo?.UserId ?? ""))
        using (LogContext.PushProperty("Username", userInfo?.Username ?? ""))
        {
            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? "";

            _logger.LogInformation("Request started: {Method} {Path}", method, path);

            try
            {
                if (userInfo != null)
                {
                    context.Items["UserInfo"] = userInfo;
                    context.User = CreateClaimsPrincipal(userInfo);
                }

                await _next(context);

                _logger.LogInformation(
                    "Request completed: {Method} {Path} -> {StatusCode}",
                    method,
                    path,
                    context.Response.StatusCode
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Request failed: {Method} {Path}", method, path);
                throw;
            }
        }
    }

    /// <summary>
    /// Extrai informações do JWT (validação de assinatura, issuer e expiração é feita pelo Kong)
    /// </summary>
    private UserInfo? ExtractUserInfo(HttpRequest request)
    {
        try
        {
            var authHeader = request.Headers.Authorization.FirstOrDefault();
            if (
                string.IsNullOrEmpty(authHeader)
                || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            )
            {
                return null;
            }

            var token = authHeader["Bearer ".Length..].Trim();
            if (string.IsNullOrEmpty(token) || !_tokenHandler.CanReadToken(token))
            {
                return null;
            }

            var jwtToken = _tokenHandler.ReadJwtToken(token);

            var userId = jwtToken.Claims.FirstOrDefault(x => x.Type == "sub")?.Value ?? "";
            var username =
                jwtToken.Claims.FirstOrDefault(x => x.Type == "preferred_username")?.Value ?? "";
            var email = jwtToken.Claims.FirstOrDefault(x => x.Type == "email")?.Value;
            var sessionId =
                jwtToken.Claims.FirstOrDefault(x => x.Type == "session_state")?.Value ?? "";
            var roles = ExtractRoles(jwtToken);

            return new UserInfo
            {
                UserId = userId,
                Username = username,
                Email = email,
                SessionId = sessionId,
                Roles = roles,
                IsAuthenticated = true,
                Token = token,
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Não foi possível extrair informações do JWT");
            return null;
        }
    }

    /// <summary>
    /// Extrai roles do Keycloak JWT
    /// </summary>
    private List<string> ExtractRoles(JwtSecurityToken jwtToken)
    {
        var roles = new List<string>();

        try
        {
            var realmAccess = jwtToken.Claims.FirstOrDefault(x => x.Type == "realm_access")?.Value;
            if (!string.IsNullOrEmpty(realmAccess) && realmAccess.Contains("\"roles\""))
            {
                var rolesStart = realmAccess.IndexOf("[");
                var rolesEnd = realmAccess.IndexOf("]");
                if (rolesStart > 0 && rolesEnd > rolesStart)
                {
                    var rolesJson = realmAccess.Substring(
                        rolesStart + 1,
                        rolesEnd - rolesStart - 1
                    );
                    var roleItems = rolesJson.Split(',');
                    foreach (var role in roleItems)
                    {
                        var cleanRole = role.Trim().Replace("\"", "");
                        if (!string.IsNullOrEmpty(cleanRole))
                            roles.Add(cleanRole);
                    }
                }
            }

            var directRoles = jwtToken
                .Claims.Where(x => x.Type == "roles" || x.Type == "role")
                .Select(x => x.Value)
                .Where(x => !string.IsNullOrEmpty(x));
            roles.AddRange(directRoles);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Erro ao extrair roles (não crítico)");
        }

        return [.. roles.Distinct()];
    }

    /// <summary>
    /// Cria ClaimsPrincipal para compatibilidade com [Authorize]
    /// </summary>
    private static ClaimsPrincipal CreateClaimsPrincipal(UserInfo userInfo)
    {
        var claims = new List<Claim>
        {
            new("sub", userInfo.UserId),
            new("preferred_username", userInfo.Username),
            new("session_state", userInfo.SessionId),
            new(ClaimTypes.NameIdentifier, userInfo.UserId),
            new(ClaimTypes.Name, userInfo.Username),
        };

        if (!string.IsNullOrEmpty(userInfo.Email))
        {
            claims.Add(new("email", userInfo.Email));
            claims.Add(new(ClaimTypes.Email, userInfo.Email));
        }

        foreach (var role in userInfo.Roles)
        {
            claims.Add(new(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "jwt");
        return new ClaimsPrincipal(identity);
    }
}

/// <summary>
/// Informações do usuário extraídas do JWT
/// </summary>
public class UserInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public bool IsAuthenticated { get; set; }
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Extensions para obter informações do usuário do contexto HTTP
/// </summary>
public static class LogContextMiddlewareExtensions
{
    public static IApplicationBuilder UseLogContext(this IApplicationBuilder builder) =>
        builder.UseMiddleware<LogContextMiddleware>();

    public static UserInfo? GetUserInfo(this HttpContext context) =>
        context.Items["UserInfo"] as UserInfo;

    public static bool IsAuthenticated(this HttpContext context) =>
        context.GetUserInfo()?.IsAuthenticated == true;

    public static string? GetUserId(this HttpContext context) => context.GetUserInfo()?.UserId;

    public static string? GetUsername(this HttpContext context) => context.GetUserInfo()?.Username;

    public static string? GetEmail(this HttpContext context) => context.GetUserInfo()?.Email;

    public static string? GetSessionId(this HttpContext context) =>
        context.GetUserInfo()?.SessionId;

    public static bool HasRole(this HttpContext context, string role) =>
        context.GetUserInfo()?.Roles?.Contains(role, StringComparer.OrdinalIgnoreCase) == true;

    public static List<string> GetRoles(this HttpContext context) =>
        context.GetUserInfo()?.Roles ?? [];

    public static string? GetToken(this HttpContext context) => context.GetUserInfo()?.Token;

    public static string? GetCorrelationId(this HttpContext context) =>
        context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? context
            .Request.Headers["X-Request-ID"]
            .FirstOrDefault();

    public static string? GetRequestId(this HttpContext context) =>
        context.Request.Headers["X-Kong-Request-ID"].FirstOrDefault();
}
