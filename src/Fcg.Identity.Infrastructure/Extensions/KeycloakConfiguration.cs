namespace Fcg.Identity.Infrastructure.Extensions;

public class KeycloakConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
    public string TokenEndpointPath { get; set; } = string.Empty;
    public string UsersEndpointPath { get; set; } = string.Empty;
    public string TargetRealm { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string AdminClientId { get; set; } = string.Empty;
    public string AdminClientSecret { get; set; } = string.Empty;
    public string ApiClientId { get; set; } = string.Empty;
    public string ApiClientSecret { get; set; } = string.Empty;
}
