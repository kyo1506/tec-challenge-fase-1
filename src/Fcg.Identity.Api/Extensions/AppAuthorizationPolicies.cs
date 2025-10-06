using Fcg.Identity.Shared.Constants;

namespace Fcg.Identity.Api.Extensions;

/// <summary>
/// Define as políticas de autorização da aplicação baseadas em scopes/permissões.
/// Apenas as 3 permissões essenciais: users:manage, users:read e profiles:manage
/// </summary>
public static class AppAuthorizationPolicies
{
    /// <summary>
    /// Mapeamento das políticas para suas respectivas permissões/scopes necessários
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string[]> Policies = new Dictionary<
        string,
        string[]
    >
    {
        { "CanManageUsers", [Permissions.Users.Manage] },
        { "CanReadUsers", [Permissions.Users.Read, Permissions.Users.Manage] },
        { "CanManageProfile", [Permissions.Profile.Manage] },
    };

    /// <summary>
    /// Todas as permissões disponíveis no sistema
    /// </summary>
    public static readonly string[] AllPermissions = Permissions.All;
}
