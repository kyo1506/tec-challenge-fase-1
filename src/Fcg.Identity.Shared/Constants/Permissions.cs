using System;

namespace Fcg.Identity.Shared.Constants;

/// <summary>
/// Constantes para as permissões/scopes do sistema.
/// Sistema simplificado com apenas 3 permissões essenciais.
/// </summary>
public static class Permissions
{
    /// <summary>
    /// Permissões relacionadas ao gerenciamento de usuários
    /// </summary>
    public static class Users
    {
        /// <summary>
        /// Permite ler informações de usuários
        /// </summary>
        public const string Read = "users:read";

        /// <summary>
        /// Permite gerenciar usuários (criar, editar, desabilitar)
        /// </summary>
        public const string Manage = "users:manage";
    }

    /// <summary>
    /// Permissões relacionadas ao gerenciamento de perfil
    /// </summary>
    public static class Profile
    {
        /// <summary>
        /// Permite gerenciar o próprio perfil (ler e editar)
        /// </summary>
        public const string Manage = "profiles:manage";
    }

    /// <summary>
    /// Array com todas as permissões disponíveis no sistema
    /// </summary>
    public static readonly string[] All = [Users.Read, Users.Manage, Profile.Manage];

    /// <summary>
    /// Verifica se uma permissão é válida
    /// </summary>
    /// <param name="permission">Permissão a ser validada</param>
    /// <returns>True se a permissão é válida, False caso contrário</returns>
    public static bool IsValid(string permission)
    {
        return Array.Exists(All, p => p == permission);
    }
}
