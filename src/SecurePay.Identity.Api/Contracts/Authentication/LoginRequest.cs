using System.ComponentModel.DataAnnotations;

namespace SecurePay.Identity.Api.Contracts.Authentication;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 12)]
    public string Password { get; init; } = string.Empty;
}