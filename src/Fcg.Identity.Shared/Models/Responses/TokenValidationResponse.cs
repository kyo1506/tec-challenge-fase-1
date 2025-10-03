using System;
using System.Collections.Generic;

namespace Fcg.Identity.Shared.Models.Responses;

/// <summary>
/// Token validation response for other microservices.
/// </summary>
public class TokenValidationResponse
{
    /// <summary>
    /// Indica se o token é válido.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Unique user ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// User email.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// List of user roles/permissions.
    /// </summary>
    public IEnumerable<string> Roles { get; set; } = new List<string>();

    /// <summary>
    /// Data e hora de expiração do token.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
