namespace SecurePay.Transactions.Api.Contracts.Operations;

public sealed class OperationsTransactionQuery
{
    public string? Status { get; init; }

    public string? TransactionType { get; init; }

    public Guid? UserId { get; init; }

    public DateTimeOffset? CreatedFromUtc { get; init; }

    public DateTimeOffset? CreatedToUtc { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
