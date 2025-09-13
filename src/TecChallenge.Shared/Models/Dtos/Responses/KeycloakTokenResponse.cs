using System.Text.Json.Serialization;

namespace TecChallenge.Shared.Models.Dtos.Responses;

public class KeycloakTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }
}
