using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecurePay.Transactions.Api.Domain.Entities;
using SecurePay.Transactions.Api.Domain.Enums;
using SecurePay.Transactions.Api.Domain.ValueObjects;
using SecurePay.Transactions.Api.Persistence;
using SecurePay.Transactions.IntegrationTests.Infrastructure;

namespace SecurePay.Transactions.IntegrationTests;

[Collection(TransactionsApiCollection.Name)]
public sealed class LedgerConstraintTests(
    TransactionsApiFixture fixture)
{
    [Fact]
    public async Task LedgerPosting_WhenTransactionIsDuplicated_RejectsSecondPosting()
    {
        var userId = Guid.NewGuid();

        var transaction = PaymentTransaction.Create(
            userId,
            TransactionType.Deposit,
            80m,
            "BWP",
            $"ledger-{Guid.NewGuid():N}",
            "Unique ledger posting test");

        var wallet = WalletAccount.Create(
            userId,
            "BWP");

        var customerAccount =
            LedgerAccount.CreateCustomerWallet(
                wallet.Id,
                "BWP");

        var clearingAccount =
            LedgerAccount.CreatePlatformClearing(
                "BWP");

        var firstPosting = LedgerPosting.Create(
            transaction.Id,
            "BWP",
            [
                new LedgerLine(
                    clearingAccount.Id,
                    LedgerEntryDirection.Debit,
                    80m),
                new LedgerLine(
                    customerAccount.Id,
                    LedgerEntryDirection.Credit,
                    80m)
            ],
            DateTimeOffset.UtcNow);

        await using (var creationScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var creationDbContext =
                creationScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            creationDbContext.PaymentTransactions.Add(
                transaction);

            creationDbContext.WalletAccounts.Add(
                wallet);

            creationDbContext.LedgerAccounts.AddRange(
                customerAccount,
                clearingAccount);

            creationDbContext.LedgerPostings.Add(
                firstPosting);

            await creationDbContext.SaveChangesAsync();
        }

        var duplicatePosting = LedgerPosting.Create(
            transaction.Id,
            "BWP",
            [
                new LedgerLine(
                    clearingAccount.Id,
                    LedgerEntryDirection.Debit,
                    80m),
                new LedgerLine(
                    customerAccount.Id,
                    LedgerEntryDirection.Credit,
                    80m)
            ],
            DateTimeOffset.UtcNow);

        await using (var duplicateScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var duplicateDbContext =
                duplicateScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            duplicateDbContext.LedgerPostings.Add(
                duplicatePosting);

            await Assert.ThrowsAsync<DbUpdateException>(
                () => duplicateDbContext.SaveChangesAsync());
        }

        await using var verificationScope =
            fixture.Factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        var persistedPostingCount =
            await verificationDbContext.LedgerPostings
                .AsNoTracking()
                .CountAsync(posting =>
                    posting.PaymentTransactionId ==
                    transaction.Id);

        Assert.Equal(
            1,
            persistedPostingCount);
    }

    [Fact]
    public async Task CustomerLedgerAccount_WhenWalletIsDuplicated_RejectsSecondAccount()
    {
        var wallet = WalletAccount.Create(
            Guid.NewGuid(),
            "NAD");

        var firstAccount =
            LedgerAccount.CreateCustomerWallet(
                wallet.Id,
                "NAD");

        await using (var creationScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var creationDbContext =
                creationScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            creationDbContext.WalletAccounts.Add(
                wallet);

            creationDbContext.LedgerAccounts.Add(
                firstAccount);

            await creationDbContext.SaveChangesAsync();
        }

        var duplicateAccount =
            LedgerAccount.CreateCustomerWallet(
                wallet.Id,
                "NAD");

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
                    account.WalletId == wallet.Id);

        Assert.Equal(
            1,
            persistedAccountCount);
    }
}