using System.Text.Json.Serialization;

namespace Fcg.Identity.Shared.Models.Responses;

public class KeycloakRoleResponse
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}
