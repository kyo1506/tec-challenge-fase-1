using System.ComponentModel.DataAnnotations;

namespace Fcg.Identity.Shared.Models.Requests;

public class RefreshTokenRequest
{
    [Required]
    public string? RefreshToken { get; set; }
}
