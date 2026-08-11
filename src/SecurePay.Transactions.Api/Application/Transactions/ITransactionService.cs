using SecurePay.Transactions.Api.Contracts.Transactions;

namespace SecurePay.Transactions.Api.Application.Transactions;

public interface ITransactionService
{
    Task<TransactionResponse> CreateAsync(
        Guid userId,
        CreateTransactionRequest request,
        CancellationToken cancellationToken);

    Task<TransactionResponse?> GetByIdAsync(
        Guid userId,
        Guid transactionId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TransactionResponse>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken);
}