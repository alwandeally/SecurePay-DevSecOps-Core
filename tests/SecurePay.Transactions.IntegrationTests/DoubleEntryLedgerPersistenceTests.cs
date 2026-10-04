using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecurePay.Transactions.Api.Domain.Entities;
using SecurePay.Transactions.Api.Domain.Enums;
using SecurePay.Transactions.Api.Domain.ValueObjects;
using SecurePay.Transactions.Api.Persistence;
using SecurePay.Transactions.IntegrationTests.Infrastructure;

namespace SecurePay.Transactions.IntegrationTests;

[Collection(TransactionsApiCollection.Name)]
public sealed class DoubleEntryLedgerPersistenceTests(
    TransactionsApiFixture fixture)
{
    [Fact]
    public async Task BalancedPosting_WhenSaved_CanBeReloadedWithEntries()
    {
        var userId = Guid.NewGuid();

        var transaction = PaymentTransaction.Create(
            userId,
            TransactionType.Deposit,
            125.50m,
            "ZAR",
            $"ledger-{Guid.NewGuid():N}",
            "Double-entry ledger persistence test");

        var wallet = WalletAccount.Create(
            userId,
            "ZAR");

        var customerAccount =
            LedgerAccount.CreateCustomerWallet(
                wallet.Id,
                "ZAR");

        var occurredAtUtc = new DateTimeOffset(
            2026,
            9,
            9,
            16,
            30,
            0,
            TimeSpan.Zero);

        Guid postingId;
        Guid clearingAccountId;

        await using (var creationScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var creationDbContext =
                creationScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            var clearingAccount =
                await creationDbContext.LedgerAccounts
                    .SingleOrDefaultAsync(account =>
                        account.AccountType ==
                            LedgerAccountType.PlatformClearing &&
                        account.Currency == "ZAR");

            if (clearingAccount is null)
            {
                clearingAccount =
                    LedgerAccount.CreatePlatformClearing(
                        "ZAR");

                creationDbContext.LedgerAccounts.Add(
                    clearingAccount);
            }

            var posting = LedgerPosting.Create(
                transaction.Id,
                "ZAR",
                [
                    new LedgerLine(
                        clearingAccount.Id,
                        LedgerEntryDirection.Debit,
                        125.50m),
                    new LedgerLine(
                        customerAccount.Id,
                        LedgerEntryDirection.Credit,
                        125.50m)
                ],
                occurredAtUtc);

            creationDbContext.PaymentTransactions.Add(
                transaction);

            creationDbContext.WalletAccounts.Add(
                wallet);

            creationDbContext.LedgerAccounts.Add(
                customerAccount);

            creationDbContext.LedgerPostings.Add(
                posting);

            await creationDbContext.SaveChangesAsync();

            postingId = posting.Id;
            clearingAccountId = clearingAccount.Id;
        }

        await using var verificationScope =
            fixture.Factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        var persistedPosting =
            await verificationDbContext.LedgerPostings
                .AsNoTracking()
                .Include(existing => existing.Entries)
                .SingleAsync(existing =>
                    existing.Id == postingId);

        Assert.Equal(
            transaction.Id,
            persistedPosting.PaymentTransactionId);

        Assert.Equal(
            "ZAR",
            persistedPosting.Currency);

        Assert.Equal(
            occurredAtUtc,
            persistedPosting.CreatedAtUtc);

        Assert.Equal(
            2,
            persistedPosting.Entries.Count);

        var debitEntry =
            persistedPosting.Entries.Single(entry =>
                entry.LedgerAccountId ==
                clearingAccountId);

        Assert.Equal(
            LedgerEntryDirection.Debit,
            debitEntry.Direction);

        Assert.Equal(
            125.50m,
            debitEntry.Amount);

        Assert.Equal(
            occurredAtUtc,
            debitEntry.CreatedAtUtc);

        var creditEntry =
            persistedPosting.Entries.Single(entry =>
                entry.LedgerAccountId ==
                customerAccount.Id);

        Assert.Equal(
            LedgerEntryDirection.Credit,
            creditEntry.Direction);

        Assert.Equal(
            125.50m,
            creditEntry.Amount);

        Assert.Equal(
            occurredAtUtc,
            creditEntry.CreatedAtUtc);
    }

    [Fact]
    public async Task PlatformClearingAccount_WhenCurrencyIsDuplicated_RejectsSecondAccount()
    {
        var firstAccount =
            LedgerAccount.CreatePlatformClearing(
                "XAF");

        await using (var creationScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var creationDbContext =
                creationScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            creationDbContext.LedgerAccounts.Add(
                firstAccount);

            await creationDbContext.SaveChangesAsync();
        }

        var duplicateAccount =
            LedgerAccount.CreatePlatformClearing(
                "xaf");

        await using (var duplicateScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var duplicateDbContext =
                duplicateScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            duplicateDbContext.LedgerAccounts.Add(
                duplicateAccount);

            await Assert.ThrowsAsync<DbUpdateException>(
                () => duplicateDbContext.SaveChangesAsync());
        }

        await using var verificationScope =
            fixture.Factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        var persistedAccountCount =
            await verificationDbContext.LedgerAccounts
                .AsNoTracking()
                .CountAsync(account =>
                    account.AccountType ==
                        LedgerAccountType.PlatformClearing &&
                    account.Currency == "XAF");

        Assert.Equal(
            1,
            persistedAccountCount);
    }
}