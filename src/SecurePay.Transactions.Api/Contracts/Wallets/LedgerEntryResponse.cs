namespace SecurePay.Transactions.Api.Contracts.Wallets;

public sealed record LedgerEntryResponse(
    Guid EntryId,
    Guid PostingId,
    Guid TransactionId,
    string TransactionReference,
    string TransactionType,
    string Direction,
    decimal Amount,
    string Currency,
    string? Description,
    DateTimeOffset CreatedAtUtc);