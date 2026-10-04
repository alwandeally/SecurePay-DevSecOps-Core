using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecurePay.Transactions.Api.Application.Transactions;
using SecurePay.Transactions.Api.Domain.Entities;
using SecurePay.Transactions.Api.Domain.Enums;
using SecurePay.Transactions.Api.Persistence;
using SecurePay.Transactions.IntegrationTests.Infrastructure;

using PaymentStatus =
    SecurePay.Transactions.Api.Domain.Enums.TransactionStatus;

namespace SecurePay.Transactions.IntegrationTests;

[Collection(TransactionsApiCollection.Name)]
public sealed class AtomicTransactionPostingTests(
    TransactionsApiFixture fixture)
{
    [Fact]
    public async Task CompleteAsync_Deposit_CreditsWalletAndCreatesBalancedPosting()
    {
        var userId = Guid.NewGuid();

        var transaction =
            PaymentTransaction.Create(
                userId,
                TransactionType.Deposit,
                200m,
                "NAD",
                $"atomic-deposit-{Guid.NewGuid():N}",
                "Atomic deposit test");

        await using (var seedScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var seedDbContext =
                seedScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            seedDbContext.PaymentTransactions.Add(
                transaction);

            await seedDbContext.SaveChangesAsync();
        }

        await using (var completionScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var completionDbContext =
                completionScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            var transactionService =
                new TransactionService(
                    completionDbContext);

            var response =
                await transactionService.CompleteAsync(
                    transaction.Id,
                    CancellationToken.None);

            Assert.NotNull(response);
            Assert.Equal(
                "Completed",
                response.Status);
        }

        await using var verificationScope =
            fixture.Factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        var persistedTransaction =
            await verificationDbContext.PaymentTransactions
                .AsNoTracking()
                .SingleAsync(existing =>
                    existing.Id == transaction.Id);

        var persistedWallet =
            await verificationDbContext.WalletAccounts
                .AsNoTracking()
                .SingleAsync(wallet =>
                    wallet.UserId == userId &&
                    wallet.Currency == "NAD");

        var persistedPosting =
            await verificationDbContext.LedgerPostings
                .AsNoTracking()
                .Include(posting => posting.Entries)
                .SingleAsync(posting =>
                    posting.PaymentTransactionId ==
                    transaction.Id);

        var customerAccount =
            await verificationDbContext.LedgerAccounts
                .AsNoTracking()
                .SingleAsync(account =>
                    account.WalletId ==
                    persistedWallet.Id);

        var clearingAccount =
            await verificationDbContext.LedgerAccounts
                .AsNoTracking()
                .SingleAsync(account =>
                    account.AccountType ==
                        LedgerAccountType.PlatformClearing &&
                    account.Currency == "NAD");

        Assert.Equal(
            PaymentStatus.Completed,
            persistedTransaction.Status);

        Assert.NotNull(
            persistedTransaction.CompletedAtUtc);

        Assert.Equal(
            200m,
            persistedWallet.Balance);

        Assert.Equal(
            2,
            persistedPosting.Entries.Count);

        var debitEntry =
            persistedPosting.Entries.Single(entry =>
                entry.LedgerAccountId ==
                clearingAccount.Id);

        Assert.Equal(
            LedgerEntryDirection.Debit,
            debitEntry.Direction);

        Assert.Equal(
            200m,
            debitEntry.Amount);

        var creditEntry =
            persistedPosting.Entries.Single(entry =>
                entry.LedgerAccountId ==
                customerAccount.Id);

        Assert.Equal(
            LedgerEntryDirection.Credit,
            creditEntry.Direction);

        Assert.Equal(
            200m,
            creditEntry.Amount);
    }

    [Fact]
    public async Task CompleteAsync_Withdrawal_DebitsWalletAndCreatesBalancedPosting()
    {
        var userId = Guid.NewGuid();

        var wallet =
            WalletAccount.Create(
                userId,
                "BWP");

        wallet.Credit(
            500m,
            DateTimeOffset.UtcNow);

        var transaction =
            PaymentTransaction.Create(
                userId,
                TransactionType.Withdrawal,
                125m,
                "BWP",
                $"atomic-withdrawal-{Guid.NewGuid():N}",
                "Atomic withdrawal test");

        await using (var seedScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var seedDbContext =
                seedScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            seedDbContext.WalletAccounts.Add(
                wallet);

            seedDbContext.PaymentTransactions.Add(
                transaction);

            await seedDbContext.SaveChangesAsync();
        }

        await using (var completionScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var completionDbContext =
                completionScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            var transactionService =
                new TransactionService(
                    completionDbContext);

            var response =
                await transactionService.CompleteAsync(
                    transaction.Id,
                    CancellationToken.None);

            Assert.NotNull(response);
            Assert.Equal(
                "Completed",
                response.Status);
        }

        await using var verificationScope =
            fixture.Factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        var persistedTransaction =
            await verificationDbContext.PaymentTransactions
                .AsNoTracking()
                .SingleAsync(existing =>
                    existing.Id == transaction.Id);

        var persistedWallet =
            await verificationDbContext.WalletAccounts
                .AsNoTracking()
                .SingleAsync(existing =>
                    existing.Id == wallet.Id);

        var persistedPosting =
            await verificationDbContext.LedgerPostings
                .AsNoTracking()
                .Include(posting => posting.Entries)
                .SingleAsync(posting =>
                    posting.PaymentTransactionId ==
                    transaction.Id);

        var customerAccount =
            await verificationDbContext.LedgerAccounts
                .AsNoTracking()
                .SingleAsync(account =>
                    account.WalletId ==
                    wallet.Id);

        var clearingAccount =
            await verificationDbContext.LedgerAccounts
                .AsNoTracking()
                .SingleAsync(account =>
                    account.AccountType ==
                        LedgerAccountType.PlatformClearing &&
                    account.Currency == "BWP");

        Assert.Equal(
            PaymentStatus.Completed,
            persistedTransaction.Status);

        Assert.Equal(
            375m,
            persistedWallet.Balance);

        Assert.Equal(
            2,
            persistedPosting.Entries.Count);

        var debitEntry =
            persistedPosting.Entries.Single(entry =>
                entry.LedgerAccountId ==
                customerAccount.Id);

        Assert.Equal(
            LedgerEntryDirection.Debit,
            debitEntry.Direction);

        Assert.Equal(
            125m,
            debitEntry.Amount);

        var creditEntry =
            persistedPosting.Entries.Single(entry =>
                entry.LedgerAccountId ==
                clearingAccount.Id);

        Assert.Equal(
            LedgerEntryDirection.Credit,
            creditEntry.Direction);

        Assert.Equal(
            125m,
            creditEntry.Amount);
    }

    [Fact]
    public async Task CompleteAsync_Transfer_MovesFundsAndCreatesBalancedPosting()
    {
        var sourceUserId = Guid.NewGuid();
        var destinationUserId = Guid.NewGuid();

        var sourceWallet =
            WalletAccount.Create(
                sourceUserId,
                "LSL");

        sourceWallet.Credit(
            800m,
            DateTimeOffset.UtcNow);

        var destinationWallet =
            WalletAccount.Create(
                destinationUserId,
                "LSL");

        destinationWallet.Credit(
            100m,
            DateTimeOffset.UtcNow);

        var transaction =
            PaymentTransaction.Create(
                sourceUserId,
                TransactionType.Transfer,
                250m,
                "LSL",
                $"atomic-transfer-{Guid.NewGuid():N}",
                "Atomic transfer test",
                destinationUserId);

        await using (var seedScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var seedDbContext =
                seedScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            seedDbContext.WalletAccounts.AddRange(
                sourceWallet,
                destinationWallet);

            seedDbContext.PaymentTransactions.Add(
                transaction);

            await seedDbContext.SaveChangesAsync();
        }

        await using (var completionScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var completionDbContext =
                completionScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            var transactionService =
                new TransactionService(
                    completionDbContext);

            var response =
                await transactionService.CompleteAsync(
                    transaction.Id,
                    CancellationToken.None);

            Assert.NotNull(response);
            Assert.Equal(
                destinationUserId,
                response.DestinationUserId);

            Assert.Equal(
                "Completed",
                response.Status);
        }

        await using var verificationScope =
            fixture.Factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        var persistedTransaction =
            await verificationDbContext.PaymentTransactions
                .AsNoTracking()
                .SingleAsync(existing =>
                    existing.Id == transaction.Id);

        var persistedSourceWallet =
            await verificationDbContext.WalletAccounts
                .AsNoTracking()
                .SingleAsync(existing =>
                    existing.Id == sourceWallet.Id);

        var persistedDestinationWallet =
            await verificationDbContext.WalletAccounts
                .AsNoTracking()
                .SingleAsync(existing =>
                    existing.Id == destinationWallet.Id);

        var persistedPosting =
            await verificationDbContext.LedgerPostings
                .AsNoTracking()
                .Include(posting => posting.Entries)
                .SingleAsync(posting =>
                    posting.PaymentTransactionId ==
                    transaction.Id);

        var sourceAccount =
            await verificationDbContext.LedgerAccounts
                .AsNoTracking()
                .SingleAsync(account =>
                    account.WalletId ==
                    sourceWallet.Id);

        var destinationAccount =
            await verificationDbContext.LedgerAccounts
                .AsNoTracking()
                .SingleAsync(account =>
                    account.WalletId ==
                    destinationWallet.Id);

        Assert.Equal(
            PaymentStatus.Completed,
            persistedTransaction.Status);

        Assert.Equal(
            550m,
            persistedSourceWallet.Balance);

        Assert.Equal(
            350m,
            persistedDestinationWallet.Balance);

        Assert.Equal(
            2,
            persistedPosting.Entries.Count);

        var debitEntry =
            persistedPosting.Entries.Single(entry =>
                entry.LedgerAccountId ==
                sourceAccount.Id);

        Assert.Equal(
            LedgerEntryDirection.Debit,
            debitEntry.Direction);

        Assert.Equal(
            250m,
            debitEntry.Amount);

        var creditEntry =
            persistedPosting.Entries.Single(entry =>
                entry.LedgerAccountId ==
                destinationAccount.Id);

        Assert.Equal(
            LedgerEntryDirection.Credit,
            creditEntry.Direction);

        Assert.Equal(
            250m,
            creditEntry.Amount);
    }

    [Fact]
    public async Task CompleteAsync_WhenWithdrawalHasInsufficientFunds_PersistsNoPartialChanges()
    {
        var userId = Guid.NewGuid();

        var wallet =
            WalletAccount.Create(
                userId,
                "SZL");

        wallet.Credit(
            25m,
            DateTimeOffset.UtcNow);

        var transaction =
            PaymentTransaction.Create(
                userId,
                TransactionType.Withdrawal,
                100m,
                "SZL",
                $"atomic-failure-{Guid.NewGuid():N}",
                "Atomic rollback test");

        await using (var seedScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var seedDbContext =
                seedScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            seedDbContext.WalletAccounts.Add(
                wallet);

            seedDbContext.PaymentTransactions.Add(
                transaction);

            await seedDbContext.SaveChangesAsync();
        }

        await using (var completionScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var completionDbContext =
                completionScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            var transactionService =
                new TransactionService(
                    completionDbContext);

            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => transactionService.CompleteAsync(
                        transaction.Id,
                        CancellationToken.None));

            Assert.Equal(
                "The wallet has insufficient funds.",
                exception.Message);
        }

        await using var verificationScope =
            fixture.Factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        var persistedTransaction =
            await verificationDbContext.PaymentTransactions
                .AsNoTracking()
                .SingleAsync(existing =>
                    existing.Id == transaction.Id);

        var persistedWallet =
            await verificationDbContext.WalletAccounts
                .AsNoTracking()
                .SingleAsync(existing =>
                    existing.Id == wallet.Id);

        var postingExists =
            await verificationDbContext.LedgerPostings
                .AsNoTracking()
                .AnyAsync(posting =>
                    posting.PaymentTransactionId ==
                    transaction.Id);

        var customerLedgerAccountExists =
            await verificationDbContext.LedgerAccounts
                .AsNoTracking()
                .AnyAsync(account =>
                    account.WalletId ==
                    wallet.Id);

        Assert.Equal(
            PaymentStatus.Pending,
            persistedTransaction.Status);

        Assert.Null(
            persistedTransaction.CompletedAtUtc);

        Assert.Equal(
            25m,
            persistedWallet.Balance);

        Assert.False(
            postingExists);

        Assert.False(
            customerLedgerAccountExists);
    }
}