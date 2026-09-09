using Microsoft.EntityFrameworkCore;
using SecurePay.Transactions.Api.Contracts.Transactions;
using SecurePay.Transactions.Api.Domain.Entities;
using SecurePay.Transactions.Api.Domain.Enums;
using SecurePay.Transactions.Api.Persistence;

namespace SecurePay.Transactions.Api.Application.Transactions;

public sealed class TransactionService(
    TransactionsDbContext dbContext)
    : ITransactionService
{
    public async Task<TransactionResponse> CreateAsync(
        Guid userId,
        CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "An authenticated user ID is required.",
                nameof(userId));
        }

        if (!Enum.TryParse<TransactionType>(
                request.TransactionType,
                ignoreCase: true,
                out var transactionType) ||
            !Enum.IsDefined(transactionType))
        {
            throw new ArgumentException(
                "Transaction type must be Deposit, Withdrawal, or Transfer.",
                nameof(request.TransactionType));
        }

        var normalizedIdempotencyKey =
            request.IdempotencyKey.Trim();

        var existingTransaction =
            await dbContext.PaymentTransactions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    transaction =>
                        transaction.UserId == userId &&
                        transaction.IdempotencyKey ==
                        normalizedIdempotencyKey,
                    cancellationToken);

        // Returning the original transaction makes retries idempotent.
        if (existingTransaction is not null)
        {
            return MapResponse(existingTransaction);
        }

        var transaction = PaymentTransaction.Create(
            userId,
            transactionType,
            request.Amount,
            request.Currency,
            normalizedIdempotencyKey,
            request.Description);

        dbContext.PaymentTransactions.Add(transaction);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Handles two identical requests reaching the API simultaneously.
            dbContext.Entry(transaction).State =
                EntityState.Detached;

            existingTransaction =
                await dbContext.PaymentTransactions
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        existing =>
                            existing.UserId == userId &&
                            existing.IdempotencyKey ==
                            normalizedIdempotencyKey,
                        cancellationToken);

            if (existingTransaction is not null)
            {
                return MapResponse(existingTransaction);
            }

            throw;
        }

        return MapResponse(transaction);
    }

    public async Task<TransactionResponse?> GetByIdAsync(
        Guid userId,
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var transaction =
            await dbContext.PaymentTransactions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    existing =>
                        existing.Id == transactionId &&
                        existing.UserId == userId,
                    cancellationToken);

        return transaction is null
            ? null
            : MapResponse(transaction);
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var transactions =
            await dbContext.PaymentTransactions
                .AsNoTracking()
                .Where(transaction =>
                    transaction.UserId == userId)
                .OrderByDescending(transaction =>
                    transaction.CreatedAtUtc)
                .Take(100)
                .ToListAsync(cancellationToken);

        return transactions
            .Select(MapResponse)
            .ToArray();
    }

    public async Task<TransactionResponse?> CompleteAsync(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var transaction =
            await dbContext.PaymentTransactions
                .SingleOrDefaultAsync(
                    existing =>
                        existing.Id == transactionId,
                    cancellationToken);

        if (transaction is null)
        {
            return null;
        }

        transaction.MarkCompleted(DateTimeOffset.UtcNow);

        await SaveLifecycleChangeAsync(
            transactionId,
            cancellationToken);

        return MapResponse(transaction);
    }

    public async Task<TransactionResponse?> FailAsync(
        Guid transactionId,
        string failureReason,
        CancellationToken cancellationToken)
    {
        var transaction =
            await dbContext.PaymentTransactions
                .SingleOrDefaultAsync(
                    existing =>
                        existing.Id == transactionId,
                    cancellationToken);

        if (transaction is null)
        {
            return null;
        }

        transaction.MarkFailed(
            failureReason,
            DateTimeOffset.UtcNow);

        await SaveLifecycleChangeAsync(
            transactionId,
            cancellationToken);

        return MapResponse(transaction);
    }
    private async Task SaveLifecycleChangeAsync(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new TransactionConcurrencyException(
                transactionId,
                exception);
        }
    }
    private static TransactionResponse MapResponse(
        PaymentTransaction transaction)
    {
        return new TransactionResponse(
            TransactionId: transaction.Id,
            UserId: transaction.UserId,
            Reference: transaction.Reference,
            TransactionType: transaction.Type.ToString(),
            Amount: transaction.Amount,
            Currency: transaction.Currency,
            Status: transaction.Status.ToString(),
            Description: transaction.Description,
            FailureReason: transaction.FailureReason,
            CreatedAtUtc: transaction.CreatedAtUtc,
            CompletedAtUtc: transaction.CompletedAtUtc,
            UpdatedAtUtc: transaction.UpdatedAtUtc);
    }
}
