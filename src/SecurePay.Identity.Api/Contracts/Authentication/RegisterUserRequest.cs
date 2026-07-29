using System.ComponentModel.DataAnnotations;

namespace SecurePay.Identity.Api.Contracts.Authentication;

public sealed class RegisterUserRequest
{
    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 12)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string LastName { get; init; } = string.Empty;
}