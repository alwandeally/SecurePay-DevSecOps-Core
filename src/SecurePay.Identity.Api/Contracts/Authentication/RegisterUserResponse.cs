namespace SecurePay.Identity.Api.Contracts.Authentication;

public sealed record RegisterUserResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    DateTimeOffset CreatedAtUtc);
