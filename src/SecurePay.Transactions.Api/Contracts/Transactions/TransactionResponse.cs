namespace SecurePay.Transactions.Api.Contracts.Transactions;

public sealed record TransactionResponse(
    Guid TransactionId,
    Guid UserId,
    string Reference,
    string TransactionType,
    decimal Amount,
    string Currency,
    string Status,
    string? Description,
    string? FailureReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
