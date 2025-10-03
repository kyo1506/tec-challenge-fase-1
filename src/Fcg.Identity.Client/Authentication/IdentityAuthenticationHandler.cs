using System.Security.Claims;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Fcg.Identity.Client.Interfaces;
using Fcg.Identity.Client.Extensions;

namespace Fcg.Identity.Client.Authentication;

/// <summary>
/// Custom authentication handler for the Identity microservice.
/// </summary>
public class IdentityAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IIdentityClient identityClient) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly IIdentityClient _identityClient = identityClient;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = ExtractTokenFromRequest();

        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            var user = await _identityClient.ValidateTokenAsync(token);

            if (user == null)
            {
                return AuthenticateResult.Fail("Invalid token");
            }

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
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, ServiceCollectionExtensions.AuthenticationScheme);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating token");
            return AuthenticateResult.Fail("Token validation failed");
        }
    }

    private string? ExtractTokenFromRequest()
    {
        if (Request.Headers.TryGetValue("Authorization", out var authorization))
        {
            if (AuthenticationHeaderValue.TryParse(authorization, out var headerValue) &&
                headerValue.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
            {
                return headerValue.Parameter;
            }
        }

        if (Request.Cookies.TryGetValue("auth_token", out var cookieToken))
        {
            return cookieToken;
        }

        if (Request.Query.TryGetValue("token", out var queryToken))
        {
            return queryToken;
        }

        return null;
    }
}