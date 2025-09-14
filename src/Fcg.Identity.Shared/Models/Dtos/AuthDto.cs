using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fcg.Identity.Shared.Models.Dtos;

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenDto
{
    [Required]
    public string? RefreshToken { get; set; }
}

public class CreateUserDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;
}

public class UserDto
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    public bool FirstAccess { get; set; }

    public bool IsDeleted { get; set; }
}

public class ChangePasswordDto
{
    public string? Token { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ConfirmEmailDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public UserTokenDto? UserToken { get; set; }
}

public class UserTokenDto
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public IEnumerable<ClaimDto>? RoleClaims { get; set; }
    public IEnumerable<ClaimDto>? UserClaims { get; set; }
    public IEnumerable<ClaimDto>? UserConfig { get; set; }
}

public class ClaimDto
{
    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;
}
