namespace SecurePay.Transactions.Api.Contracts.Wallets;

public sealed record PagedLedgerEntriesResponse(
    string Currency,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<LedgerEntryResponse> Items);