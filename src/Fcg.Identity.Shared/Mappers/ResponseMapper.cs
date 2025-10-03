using Fcg.Identity.Shared.Models.Responses;

namespace Fcg.Identity.Shared.Mappers;

public static class ResponseMapper
{
    public static TokenResponse ToTokenResponse(this KeycloakTokenResponse keycloakTokenResponse)
    {
        return new TokenResponse
        {
            AccessToken = keycloakTokenResponse.AccessToken,
            ExpiresIn = keycloakTokenResponse.ExpiresIn,
            RefreshExpiresIn = keycloakTokenResponse.ExpiresIn,
            RefreshToken = keycloakTokenResponse.RefreshToken,
            TokenType = keycloakTokenResponse.TokenType,
            NotBeforePolicy = keycloakTokenResponse.NotBeforePolicy,
            SessionState = keycloakTokenResponse.SessionState,
            Scope = keycloakTokenResponse.Scope,
        };
    }
}
