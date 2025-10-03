using System.ComponentModel.DataAnnotations;

namespace Fcg.Identity.Shared.Models.Requests;

/// <summary>
/// Request model for permission validation.
/// </summary>
public class PermissionValidationRequest
{
    /// <summary>
    /// Resource name (e.g.: "users", "games", "orders").
    /// </summary>
    [Required(ErrorMessage = "Resource is required")]
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Action to validate (e.g.: "read", "write", "delete").
    /// </summary>
    [Required(ErrorMessage = "Action is required")]
    public string Action { get; set; } = string.Empty;
}