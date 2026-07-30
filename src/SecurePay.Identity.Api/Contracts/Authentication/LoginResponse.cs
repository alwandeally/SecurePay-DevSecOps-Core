namespace SecurePay.Identity.Api.Contracts.Authentication;

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAtUtc);