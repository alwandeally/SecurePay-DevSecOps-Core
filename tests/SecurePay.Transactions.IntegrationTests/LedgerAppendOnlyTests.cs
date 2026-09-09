using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecurePay.Transactions.Api.Domain.Entities;
using SecurePay.Transactions.Api.Domain.Enums;
using SecurePay.Transactions.Api.Domain.ValueObjects;
using SecurePay.Transactions.Api.Persistence;
using SecurePay.Transactions.IntegrationTests.Infrastructure;

namespace SecurePay.Transactions.IntegrationTests;

[Collection(TransactionsApiCollection.Name)]
public sealed class LedgerAppendOnlyTests(
    TransactionsApiFixture fixture)
{
    [Fact]
    public async Task LedgerAccount_WhenModified_RejectsChange()
    {
        var account =
            LedgerAccount.CreatePlatformClearing(
                "KES");

        await using (var creationScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var creationDbContext =
                creationScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            creationDbContext.LedgerAccounts.Add(
                account);

            await creationDbContext.SaveChangesAsync();
        }

        await using var modificationScope =
            fixture.Factory.Services.CreateAsyncScope();

        var modificationDbContext =
            modificationScope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        var persistedAccount =
            await modificationDbContext.LedgerAccounts
                .SingleAsync(existing =>
                    existing.Id == account.Id);

        var accountEntry =
            modificationDbContext.Entry(
                persistedAccount);

        accountEntry.Property(
                existing => existing.Currency)
            .CurrentValue = "UGX";

        accountEntry.State =
            EntityState.Modified;

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => modificationDbContext.SaveChangesAsync());

        Assert.Contains(
            nameof(LedgerAccount),
            exception.Message);

        Assert.Contains(
            "append-only",
            exception.Message);
    }

    [Fact]
    public async Task LedgerPosting_WhenDeleted_RejectsDeletion()
    {
        var seededLedger =
            await SeedPostingAsync(
                "GHS");

        await using var deletionScope =
            fixture.Factory.Services.CreateAsyncScope();

        var deletionDbContext =
            deletionScope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        var persistedPosting =
            await deletionDbContext.LedgerPostings
                .SingleAsync(posting =>
                    posting.Id ==
                    seededLedger.PostingId);

        deletionDbContext.LedgerPostings.Remove(
            persistedPosting);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => deletionDbContext.SaveChangesAsync());

        Assert.Contains(
            nameof(LedgerPosting),
            exception.Message);

        Assert.Contains(
            "append-only",
            exception.Message);
    }

    [Fact]
    public async Task LedgerEntry_When_WhenModified_RejectsChange()
    {
        var seededLedger =
            await SeedPostingAsync(
                "MZN");

        await using var modificationScope =
            fixture.Factory.Services.CreateAsyncScope();

        var modificationDbContext =
            modificationScope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        var persistedEntry =
            await modificationDbContext.LedgerEntries
                .SingleAsync(entry =>
                    entry.Id ==
                    seededLedger.EntryId);

        var entryState =
            modificationDbContext.Entry(
                persistedEntry);

        entryState.Property(
                entry => entry.Amount)
            .CurrentValue = 99m;

        entryState.State =
            EntityState.Modified;

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => modificationDbContext.SaveChangesAsync());

        Assert.Contains(
            nameof(LedgerEntry),
            exception.Message);

        Assert.Contains(
            "append-only",
            exception.Message);
    }

    private async Task<SeededLedger> SeedPostingAsync(
        string currency)
    {
        var userId = Guid.NewGuid();

        var transaction = PaymentTransaction.Create(
            userId,
            TransactionType.Deposit,
            40m,
            currency,
            $"append-only-{Guid.NewGuid():N}",
            "Append-only ledger test");

        var wallet = WalletAccount.Create(
            userId,
            currency);

        var customerAccount =
            LedgerAccount.CreateCustomerWallet(
                wallet.Id,
                currency);

        var clearingAccount =
            LedgerAccount.CreatePlatformClearing(
                currency);

        var posting = LedgerPosting.Create(
            transaction.Id,
            currency,
            [
                new LedgerLine(
                    clearingAccount.Id,
                    LedgerEntryDirection.Debit,
                    40m),
                new LedgerLine(
                    customerAccount.Id,
                    LedgerEntryDirection.Credit,
                    40m)
            ],
            DateTimeOffset.UtcNow);

        await using var creationScope =
            fixture.Factory.Services.CreateAsyncScope();

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
            posting);

        await creationDbContext.SaveChangesAsync();

        return new SeededLedger(
            posting.Id,
            posting.Entries.First().Id);
    }

    private sealed record SeededLedger(
        Guid PostingId,
        Guid EntryId);
}