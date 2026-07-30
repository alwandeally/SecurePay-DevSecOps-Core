namespace SecurePay.Identity.Api.Application.Authentication;

public sealed class AccountLockedException(
    DateTimeOffset retryAfterUtc)
    : Exception("Authentication is temporarily unavailable.")
{
    public DateTimeOffset RetryAfterUtc { get; } =
        retryAfterUtc;
}
