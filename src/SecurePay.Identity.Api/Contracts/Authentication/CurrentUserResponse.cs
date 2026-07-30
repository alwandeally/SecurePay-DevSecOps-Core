namespace SecurePay.Identity.Api.Contracts.Authentication;

public sealed record CurrentUserResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role);