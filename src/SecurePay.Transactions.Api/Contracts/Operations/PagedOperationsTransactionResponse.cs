using SecurePay.Transactions.Api.Contracts.Transactions;

namespace SecurePay.Transactions.Api.Contracts.Operations;

public sealed record PagedOperationsTransactionResponse(
    IReadOnlyList<TransactionResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
