using System;
using System.Collections.Generic;

namespace Fcg.Identity.Shared.Models.Responses;

public class UserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public IEnumerable<string> Roles { get; set; } = new List<string>();
    public bool IsEnabled { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
