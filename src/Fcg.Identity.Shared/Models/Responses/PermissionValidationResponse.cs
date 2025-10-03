using System;
using System.Collections.Generic;

namespace Fcg.Identity.Shared.Models.Responses;

/// <summary>
/// Permission validation response for other microservices.
/// </summary>
public class PermissionValidationResponse
{
    /// <summary>
    /// Indicates if the user has the requested permission.
    /// </summary>
    public bool HasPermission { get; set; }

    /// <summary>
    /// Unique user ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Recurso solicitado (ex: "users", "games", "orders").
    /// </summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Ação solicitada (ex: "read", "write", "delete").
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// List of user roles.
    /// </summary>
    public IEnumerable<string> UserRoles { get; set; } = new List<string>();
}
