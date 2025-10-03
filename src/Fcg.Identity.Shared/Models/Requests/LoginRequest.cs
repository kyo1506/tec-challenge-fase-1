using System.ComponentModel.DataAnnotations;
using Fcg.Identity.Shared.ValidationAttributes;

namespace Fcg.Identity.Shared.Models.Requests;

public class LoginRequest
{
    public string? Username { get; set; }

    [RequiredIf(
        nameof(Username),
        "",
        ErrorMessage = "Enter username or email, filling in one of the two is mandatory."
    )]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    public string Password { get; set; } = string.Empty;
}
