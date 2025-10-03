using System.ComponentModel.DataAnnotations;

namespace Fcg.Identity.Shared.Models.Requests;

public class UpdateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public string? Role { get; set; }

    public bool IsEnabled { get; set; }
}
