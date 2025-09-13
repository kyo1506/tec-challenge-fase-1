using System.Text.Json.Serialization;

namespace TecChallenge.Shared.Models.Dtos.Responses;

public class KeycloakCredentialResponse
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "password";

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("temporary")]
    public bool Temporary { get; set; } = false;
}
