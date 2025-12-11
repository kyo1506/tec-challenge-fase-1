using System.IdentityModel.Tokens.Jwt;
using Serilog.Context;

namespace Fcg.Identity.Api.Middlewares;

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
            context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? throw new InvalidOperationException("Correlation ID is missing in the request.");

        // Garante que o correlationId seja devolvido ao client
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        var userInfo = ExtractUserInfo(context);

        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("UserId", userInfo?.UserId ?? ""))
        using (LogContext.PushProperty("Username", userInfo?.Username ?? ""))
        using (LogContext.PushProperty("SessionId", userInfo?.SessionId ?? ""))
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

    private UserInfo? ExtractUserInfo(HttpContext context)
    {
        try
        {
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (
                string.IsNullOrWhiteSpace(authHeader)
                || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            )
                return null;

            var token = authHeader["Bearer ".Length..].Trim();

            if (!_tokenHandler.CanReadToken(token))
                return null;

            var jwt = _tokenHandler.ReadJwtToken(token);

            return new UserInfo
            {
                UserId = jwt.Claims.FirstOrDefault(x => x.Type == "sub")?.Value ?? "",
                Username =
                    jwt.Claims.FirstOrDefault(x => x.Type == "preferred_username")?.Value ?? "",
                SessionId = jwt.Claims.FirstOrDefault(x => x.Type == "session_state")?.Value ?? "",
            };
        }
        catch
        {
            return null;
        }
    }
}

public class UserInfo
{
    public string UserId { get; set; } = "";
    public string Username { get; set; } = "";
    public string SessionId { get; set; } = "";
}

public static class LogContextMiddlewareExtensions
{
    public static IApplicationBuilder UseLogContext(this IApplicationBuilder builder) =>
        builder.UseMiddleware<LogContextMiddleware>();
}
