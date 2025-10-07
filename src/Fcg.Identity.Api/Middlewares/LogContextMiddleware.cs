using System.IdentityModel.Tokens.Jwt;
using Serilog.Context;

namespace Fcg.Identity.Api.Middlewares;

/// <summary>
/// Middleware focado em enriquecer logs com contexto de requisição e usuário.
///
/// RequestId: Identificador único da requisição HTTP (gerado pelo Kong ou API)
/// CorrelationId: Identificador para rastrear operações distribuídas entre múltiplos serviços
///
/// Extrai informações do JWT (se presente) e adiciona ao contexto de log do Serilog.
/// </summary>
public class LogContextMiddleware(
    RequestDelegate next,
    ILogger<LogContextMiddleware> logger,
    IConfiguration configuration
)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<LogContextMiddleware> _logger = logger;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly string? _expectedIssuer = configuration["Jwt:Issuer"];

    public async Task InvokeAsync(HttpContext context)
    {
        // RequestId: Identificador único da requisição (gerado pelo Kong ou API)
        var requestId =
            context.Request.Headers["X-Kong-Request-ID"].FirstOrDefault() ?? Guid.NewGuid()
                .ToString("N")[..8];

        // CorrelationId: Identificador para rastrear operações distribuídas
        var correlationId =
            context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? context
                .Request.Headers["X-Request-ID"]
                .FirstOrDefault()
            ?? Guid.NewGuid().ToString("N")[..12];

        var userInfo = ExtractUserInfoFromJwt(context.Request);

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
    /// Extrai informações básicas do JWT apenas para contexto de log.
    /// Não faz validação completa de segurança - apenas para enriquecimento de logs.
    /// </summary>
    private SimpleLogUserInfo? ExtractUserInfoFromJwt(HttpRequest request)
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

            if (!string.IsNullOrEmpty(_expectedIssuer))
            {
                var issuer = jwtToken.Claims.FirstOrDefault(x => x.Type == "iss")?.Value;
                if (issuer != _expectedIssuer)
                {
                    _logger.LogDebug("JWT issuer mismatch for logging context");
                    return null;
                }
            }

            var userId = jwtToken.Claims.FirstOrDefault(x => x.Type == "sub")?.Value ?? "";
            var username =
                jwtToken.Claims.FirstOrDefault(x => x.Type == "preferred_username")?.Value ?? "";
            var sessionId =
                jwtToken.Claims.FirstOrDefault(x => x.Type == "session_state")?.Value ?? "";

            return new SimpleLogUserInfo
            {
                UserId = userId,
                Username = username,
                SessionId = sessionId,
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Não foi possível extrair informações do JWT para logs");
            return null;
        }
    }
}

/// <summary>
/// Informações básicas do usuário apenas para contexto de log
/// </summary>
public class SimpleLogUserInfo
{
    public string UserId { get; set; } = "";
    public string Username { get; set; } = "";
    public string SessionId { get; set; } = "";
}

/// <summary>
/// Extension method para facilitar registro do middleware
/// </summary>
public static class LogContextMiddlewareExtensions
{
    public static IApplicationBuilder UseLogContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LogContextMiddleware>();
    }
}
