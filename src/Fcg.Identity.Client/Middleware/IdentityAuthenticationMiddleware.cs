using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Fcg.Identity.Client.Interfaces;
using Fcg.Identity.Client.Extensions;

namespace Fcg.Identity.Client.Middleware;

/// <summary>
/// Middleware para autenticação automática usando o microserviço de identidade.
/// </summary>
public class IdentityAuthenticationMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, IIdentityClient identityClient)
    {
        var token = ExtractTokenFromRequest(context.Request);

        if (!string.IsNullOrEmpty(token))
        {
            var user = await identityClient.ValidateTokenAsync(token);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new(ClaimTypes.Name, user.Username),
                    new(ClaimTypes.Email, user.Email),
                    new(ClaimTypes.GivenName, user.FirstName),
                    new(ClaimTypes.Surname, user.LastName)
                };

                foreach (var role in user.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var identity = new ClaimsIdentity(claims, ServiceCollectionExtensions.AuthenticationScheme);
                context.User = new ClaimsPrincipal(identity);
            }
        }

        await _next(context);
    }

    private static string? ExtractTokenFromRequest(HttpRequest request)
    {
        var authHeader = request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader["Bearer ".Length..].Trim();
        }

        if (request.Cookies.TryGetValue("auth_token", out var cookieToken))
        {
            return cookieToken;
        }

        if (request.Query.TryGetValue("token", out var queryToken))
        {
            return queryToken;
        }

        return null;
    }
}