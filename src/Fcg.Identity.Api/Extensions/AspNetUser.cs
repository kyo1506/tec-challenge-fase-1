using System.Security.Claims;

namespace Fcg.Identity.Api.Extensions;

public class AspNetUser(IHttpContextAccessor accessor) : IUser
{
    private readonly IHttpContextAccessor _accessor = accessor;

    public string? Name => _accessor.HttpContext?.User.Identity?.Name;

    public bool IsAuthenticated()
    {
        return _accessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
    }

    public Guid GetUserId()
    {
        var userIdClaim = _accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return IsAuthenticated() && !string.IsNullOrEmpty(userIdClaim)
            ? Guid.Parse(userIdClaim)
            : Guid.Empty;
    }

    public string? GetUserEmail()
    {
        return IsAuthenticated()
            ? _accessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value
            : string.Empty;
    }

    public bool IsInRole(string role)
    {
        return _accessor.HttpContext?.User.IsInRole(role) ?? false;
    }

    public IEnumerable<Claim> GetClaimsIdentity()
    {
        return _accessor.HttpContext?.User.Claims ?? Enumerable.Empty<Claim>();
    }
}
