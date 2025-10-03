namespace Fcg.Identity.Client.Models;

/// <summary>
/// Authenticated user information.
/// </summary>
public class AuthenticatedUser
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public IEnumerable<string> Roles { get; set; } = new List<string>();
    public DateTime? ExpiresAt { get; set; }
}