using Microsoft.EntityFrameworkCore;
using SecurePay.Transactions.Api.Contracts.Transactions;
using SecurePay.Transactions.Api.Domain.Entities;
using SecurePay.Transactions.Api.Domain.Enums;
using SecurePay.Transactions.Api.Domain.ValueObjects;
using SecurePay.Transactions.Api.Persistence;

using PaymentStatus =
    SecurePay.Transactions.Api.Domain.Enums.TransactionStatus;

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
            request.Description,
            request.DestinationUserId);

        dbContext.PaymentTransactions.Add(transaction);

        try
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
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

        EnsurePending(transaction);

        var occurredAtUtc =
            DateTimeOffset.UtcNow;

        var posting =
            transaction.Type switch
            {
                TransactionType.Deposit =>
                    await CreateDepositPostingAsync(
                        transaction,
                        occurredAtUtc,
                        cancellationToken),

                TransactionType.Withdrawal =>
                    await CreateWithdrawalPostingAsync(
                        transaction,
                        occurredAtUtc,
                        cancellationToken),

                TransactionType.Transfer =>
                    await CreateTransferPostingAsync(
                        transaction,
                        occurredAtUtc,
                        cancellationToken),

                _ => throw new InvalidOperationException(
                    $"Transaction type '{transaction.Type}' cannot be posted.")
            };

        transaction.MarkCompleted(occurredAtUtc);

        dbContext.LedgerPostings.Add(posting);

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

    private async Task<LedgerPosting>
        CreateDepositPostingAsync(
            PaymentTransaction transaction,
            DateTimeOffset occurredAtUtc,
            CancellationToken cancellationToken)
    {
        var wallet =
            await GetOrCreateWalletAsync(
                transaction.UserId,
                transaction.Currency,
                cancellationToken);

        var customerAccount =
            await GetOrCreateCustomerLedgerAccountAsync(
                wallet,
                cancellationToken);

        var clearingAccount =
            await GetOrCreatePlatformClearingAccountAsync(
                transaction.Currency,
                cancellationToken);

        wallet.Credit(
            transaction.Amount,
            occurredAtUtc);

        return CreatePosting(
            transaction,
            debitAccount: clearingAccount,
            creditAccount: customerAccount,
            occurredAtUtc);
    }

    private async Task<LedgerPosting>
        CreateWithdrawalPostingAsync(
            PaymentTransaction transaction,
            DateTimeOffset occurredAtUtc,
            CancellationToken cancellationToken)
    {
        var wallet =
            await GetRequiredWalletAsync(
                transaction.UserId,
                transaction.Currency,
                cancellationToken);

        var customerAccount =
            await GetOrCreateCustomerLedgerAccountAsync(
                wallet,
                cancellationToken);

        var clearingAccount =
            await GetOrCreatePlatformClearingAccountAsync(
                transaction.Currency,
                cancellationToken);

        wallet.Debit(
            transaction.Amount,
            occurredAtUtc);

        return CreatePosting(
            transaction,
            debitAccount: customerAccount,
            creditAccount: clearingAccount,
            occurredAtUtc);
    }

    private async Task<LedgerPosting>
        CreateTransferPostingAsync(
            PaymentTransaction transaction,
            DateTimeOffset occurredAtUtc,
            CancellationToken cancellationToken)
    {
        var destinationUserId =
            transaction.DestinationUserId ??
            throw new InvalidOperationException(
                "A transfer requires a destination user.");

        var sourceWallet =
            await GetRequiredWalletAsync(
                transaction.UserId,
                transaction.Currency,
                cancellationToken);

        var destinationWallet =
            await GetRequiredWalletAsync(
                destinationUserId,
                transaction.Currency,
                cancellationToken);

        if (transaction.Amount > sourceWallet.Balance)
        {
            throw new InvalidOperationException(
                "The wallet has insufficient funds.");
        }

        if (transaction.Amount >
            WalletAccount.MaximumBalance -
            destinationWallet.Balance)
        {
            throw new InvalidOperationException(
                "The destination wallet balance limit would be exceeded.");
        }

        var sourceAccount =
            await GetOrCreateCustomerLedgerAccountAsync(
                sourceWallet,
                cancellationToken);

        var destinationAccount =
            await GetOrCreateCustomerLedgerAccountAsync(
                destinationWallet,
                cancellationToken);

        sourceWallet.Debit(
            transaction.Amount,
            occurredAtUtc);

        destinationWallet.Credit(
            transaction.Amount,
            occurredAtUtc);

        return CreatePosting(
            transaction,
            debitAccount: sourceAccount,
            creditAccount: destinationAccount,
            occurredAtUtc);
    }

    private async Task<WalletAccount>
        GetOrCreateWalletAsync(
            Guid userId,
            string currency,
            CancellationToken cancellationToken)
    {
        var wallet =
            await FindWalletAsync(
                userId,
                currency,
                cancellationToken);

        if (wallet is not null)
        {
            return wallet;
        }

        wallet = WalletAccount.Create(
            userId,
            currency);

        dbContext.WalletAccounts.Add(wallet);

        return wallet;
    }

    private async Task<WalletAccount>
        GetRequiredWalletAsync(
            Guid userId,
            string currency,
            CancellationToken cancellationToken)
    {
        var wallet =
            await FindWalletAsync(
                userId,
                currency,
                cancellationToken);

        return wallet ??
            throw new InvalidOperationException(
                $"A {currency} wallet for user '{userId}' was not found.");
    }

    private Task<WalletAccount?> FindWalletAsync(
        Guid userId,
        string currency,
        CancellationToken cancellationToken)
    {
        return dbContext.WalletAccounts
            .SingleOrDefaultAsync(
                wallet =>
                    wallet.UserId == userId &&
                    wallet.Currency == currency,
                cancellationToken);
    }

    private async Task<LedgerAccount>
        GetOrCreateCustomerLedgerAccountAsync(
            WalletAccount wallet,
            CancellationToken cancellationToken)
    {
        var account =
            await dbContext.LedgerAccounts
                .SingleOrDefaultAsync(
                    existing =>
                        existing.WalletId == wallet.Id,
                    cancellationToken);

        if (account is null)
        {
            account =
                LedgerAccount.CreateCustomerWallet(
                    wallet.Id,
                    wallet.Currency);

            dbContext.LedgerAccounts.Add(account);

            return account;
        }

        if (account.AccountType !=
                LedgerAccountType.CustomerWallet ||
            account.Currency != wallet.Currency)
        {
            throw new InvalidOperationException(
                "The wallet ledger account is inconsistent with the wallet.");
        }

        return account;
    }

    private async Task<LedgerAccount>
        GetOrCreatePlatformClearingAccountAsync(
            string currency,
            CancellationToken cancellationToken)
    {
        var account =
            await dbContext.LedgerAccounts
                .SingleOrDefaultAsync(
                    existing =>
                        existing.AccountType ==
                            LedgerAccountType.PlatformClearing &&
                        existing.Currency == currency,
                    cancellationToken);

        if (account is not null)
        {
            return account;
        }

        account =
            LedgerAccount.CreatePlatformClearing(
                currency);

        dbContext.LedgerAccounts.Add(account);

        return account;
    }

    private static LedgerPosting CreatePosting(
        PaymentTransaction transaction,
        LedgerAccount debitAccount,
        LedgerAccount creditAccount,
        DateTimeOffset occurredAtUtc)
    {
        if (debitAccount.Currency !=
                transaction.Currency ||
            creditAccount.Currency !=
                transaction.Currency)
        {
            throw new InvalidOperationException(
                "Every ledger account must use the transaction currency.");
        }

        LedgerLine[] lines =
        [
            new(
                debitAccount.Id,
                LedgerEntryDirection.Debit,
                transaction.Amount),

            new(
                creditAccount.Id,
                LedgerEntryDirection.Credit,
                transaction.Amount)
        ];

        return LedgerPosting.Create(
            transaction.Id,
            transaction.Currency,
            lines,
            occurredAtUtc);
    }

    private static void EnsurePending(
        PaymentTransaction transaction)
    {
        if (transaction.Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException(
                $"A transaction with status '{transaction.Status}' cannot be changed.");
        }
    }

    private async Task SaveLifecycleChangeAsync(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        try
        {
            // EF Core wraps this multi-entity save in one database
            // transaction, so wallet, ledger, and lifecycle changes
            // either all commit or all roll back.
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
            DestinationUserId:
                transaction.DestinationUserId,
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