using System.Text.Json.Serialization;

namespace Fcg.Identity.Shared.Models.Dtos.Responses;

public class KeycloakRoleResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
