using SecurePay.Transactions.Api.Contracts.Operations;

namespace SecurePay.Transactions.Api.Application.Operations;

public interface IOperationsTransactionQueryService
{
    Task<PagedOperationsTransactionResponse> QueryAsync(
        OperationsTransactionQuery query,
        CancellationToken cancellationToken);
}
