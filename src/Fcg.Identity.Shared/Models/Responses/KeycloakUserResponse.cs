using System;
using System.Collections.Generic;

namespace Fcg.Identity.Shared.Models.Responses;

public class KeycloakUserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool EmailVerified { get; set; }
}
