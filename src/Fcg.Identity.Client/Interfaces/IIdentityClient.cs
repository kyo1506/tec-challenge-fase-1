using Fcg.Identity.Client.Models;

namespace Fcg.Identity.Client.Interfaces;

/// <summary>
/// Interface para validação de autenticação e autorização com o microserviço de identidade.
/// </summary>
public interface IIdentityClient
{
    /// <summary>
    /// Valida um token JWT.
    /// </summary>
    /// <param name="token">Token JWT a ser validado</param>
    /// <returns>Informações do usuário se válido, caso contrário null</returns>
    Task<AuthenticatedUser?> ValidateTokenAsync(string token);
    
    /// <summary>
    /// Valida se um usuário tem permissão específica.
    /// </summary>
    /// <param name="token">Token JWT do usuário</param>
    /// <param name="resource">Recurso solicitado</param>
    /// <param name="action">Ação solicitada</param>
    /// <returns>True se tem permissão, caso contrário false</returns>
    Task<bool> ValidatePermissionAsync(string token, string resource, string action);
    
    /// <summary>
    /// Verifica se o serviço de identidade está funcionando.
    /// </summary>
    /// <returns>True se está saudável, caso contrário false</returns>
    Task<bool> IsHealthyAsync();
}