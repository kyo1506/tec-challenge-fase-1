using System.Security.Claims;
using Fcg.Identity.Client.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Identity.Client.Attributes;

/// <summary>
/// Attribute for validating specific permissions using the identity microservice.
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public class RequirePermissionAttribute(string resource, string action)
    : Attribute,
        IAsyncAuthorizationFilter
{
    private readonly string _resource = resource;
    private readonly string _action = action;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var identityClient =
            context.HttpContext.RequestServices.GetRequiredService<IIdentityClient>();
        if (identityClient == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        var token = ExtractTokenFromContext(context.HttpContext);
        if (string.IsNullOrEmpty(token))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        try
        {
            var hasPermission = await identityClient.ValidatePermissionAsync(
                token,
                _resource,
                _action
            );

            if (!hasPermission)
            {
                context.Result = new ForbidResult();
            }
        }
        catch
        {
            context.Result = new StatusCodeResult(500);
        }
    }

    private static string? ExtractTokenFromContext(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (
            !string.IsNullOrEmpty(authHeader)
            && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        )
        {
            return authHeader["Bearer ".Length..].Trim();
        }

        return null;
    }
}
