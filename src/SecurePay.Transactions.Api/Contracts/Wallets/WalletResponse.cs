namespace SecurePay.Transactions.Api.Contracts.Wallets;

public sealed record WalletResponse(
    Guid WalletId,
    string Currency,
    decimal Balance,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);