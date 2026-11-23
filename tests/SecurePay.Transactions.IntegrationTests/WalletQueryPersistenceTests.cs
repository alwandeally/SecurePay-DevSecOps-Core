using Microsoft.Extensions.DependencyInjection;
using SecurePay.Transactions.Api.Application.Wallets;
using SecurePay.Transactions.Api.Domain.Entities;
using SecurePay.Transactions.Api.Domain.Enums;
using SecurePay.Transactions.Api.Domain.ValueObjects;
using SecurePay.Transactions.Api.Persistence;
using SecurePay.Transactions.IntegrationTests.Infrastructure;

namespace SecurePay.Transactions.IntegrationTests;

[Collection(TransactionsApiCollection.Name)]
public sealed class WalletQueryPersistenceTests(
    TransactionsApiFixture fixture)
{
    [Fact]
    public async Task GetAllAsync_WhenUsersHaveWallets_ReturnsOnlyRequestedUsersWallets()
    {
        var requestedUserId =
            Guid.NewGuid();

        var otherUserId =
            Guid.NewGuid();

        var requestedUserZarWallet =
            WalletAccount.Create(
                requestedUserId,
                "ZAR");

        var requestedUserUsdWallet =
            WalletAccount.Create(
                requestedUserId,
                "USD");

        var otherUserWallet =
            WalletAccount.Create(
                otherUserId,
                "EUR");

        await using (var creationScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var dbContext =
                creationScope.ServiceProvider
                    .GetRequiredService<
                        TransactionsDbContext>();

            dbContext.WalletAccounts.AddRange(
                requestedUserZarWallet,
                requestedUserUsdWallet,
                otherUserWallet);

            await dbContext.SaveChangesAsync();
        }

        await using var queryScope =
            fixture.Factory.Services.CreateAsyncScope();

        var queryService =
            queryScope.ServiceProvider
                .GetRequiredService<
                    IWalletQueryService>();

        var wallets =
            await queryService.GetAllAsync(
                requestedUserId,
                CancellationToken.None);

        Assert.Equal(
            2,
            wallets.Count);

        Assert.Collection(
            wallets,
            wallet =>
            {
                Assert.Equal(
                    requestedUserUsdWallet.Id,
                    wallet.WalletId);

                Assert.Equal(
                    "USD",
                    wallet.Currency);
            },
            wallet =>
            {
                Assert.Equal(
                    requestedUserZarWallet.Id,
                    wallet.WalletId);

                Assert.Equal(
                    "ZAR",
                    wallet.Currency);
            });

        Assert.DoesNotContain(
            wallets,
            wallet =>
                wallet.WalletId ==
                otherUserWallet.Id);
    }

    [Fact]
    public async Task GetLedgerEntriesAsync_WhenEntriesExist_ReturnsOnlyOwnersPagedEntries()
    {
        const string currency = "QXZ";

        var ownerUserId =
            Guid.NewGuid();

        var otherUserId =
            Guid.NewGuid();

        var ownerWallet =
            WalletAccount.Create(
                ownerUserId,
                currency);

        var otherWallet =
            WalletAccount.Create(
                otherUserId,
                currency);

        var ownerLedgerAccount =
            LedgerAccount.CreateCustomerWallet(
                ownerWallet.Id,
                currency);

        var otherLedgerAccount =
            LedgerAccount.CreateCustomerWallet(
                otherWallet.Id,
                currency);

        var clearingAccount =
            LedgerAccount.CreatePlatformClearing(
                currency);

        var firstTransaction =
            PaymentTransaction.Create(
                ownerUserId,
                TransactionType.Deposit,
                100.00m,
                currency,
                $"wallet-query-first-{Guid.NewGuid():N}",
                "First wallet query transaction");

        var secondTransaction =
            PaymentTransaction.Create(
                ownerUserId,
                TransactionType.Deposit,
                250.00m,
                currency,
                $"wallet-query-second-{Guid.NewGuid():N}",
                "Second wallet query transaction");

        var firstOccurredAtUtc =
            new DateTimeOffset(
                2026,
                11,
                22,
                8,
                0,
                0,
                TimeSpan.Zero);

        var secondOccurredAtUtc =
            firstOccurredAtUtc.AddMinutes(10);

        var firstPosting =
            LedgerPosting.Create(
                firstTransaction.Id,
                currency,
                [
                    new LedgerLine(
                        clearingAccount.Id,
                        LedgerEntryDirection.Debit,
                        100.00m),
                    new LedgerLine(
                        ownerLedgerAccount.Id,
                        LedgerEntryDirection.Credit,
                        100.00m)
                ],
                firstOccurredAtUtc);

        var secondPosting =
            LedgerPosting.Create(
                secondTransaction.Id,
                currency,
                [
                    new LedgerLine(
                        clearingAccount.Id,
                        LedgerEntryDirection.Debit,
                        250.00m),
                    new LedgerLine(
                        ownerLedgerAccount.Id,
                        LedgerEntryDirection.Credit,
                        250.00m)
                ],
                secondOccurredAtUtc);

        await using (var creationScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var dbContext =
                creationScope.ServiceProvider
                    .GetRequiredService<
                        TransactionsDbContext>();

            dbContext.PaymentTransactions.AddRange(
                firstTransaction,
                secondTransaction);

            dbContext.WalletAccounts.AddRange(
                ownerWallet,
                otherWallet);

            dbContext.LedgerAccounts.AddRange(
                ownerLedgerAccount,
                otherLedgerAccount,
                clearingAccount);

            dbContext.LedgerPostings.AddRange(
                firstPosting,
                secondPosting);

            await dbContext.SaveChangesAsync();
        }

        await using var queryScope =
            fixture.Factory.Services.CreateAsyncScope();

        var queryService =
            queryScope.ServiceProvider
                .GetRequiredService<
                    IWalletQueryService>();

        var firstPage =
            await queryService.GetLedgerEntriesAsync(
                ownerUserId,
                currency.ToLowerInvariant(),
                page: 1,
                pageSize: 1,
                CancellationToken.None);

        Assert.NotNull(firstPage);

        Assert.Equal(
            currency,
            firstPage.Currency);

        Assert.Equal(
            1,
            firstPage.Page);

        Assert.Equal(
            1,
            firstPage.PageSize);

        Assert.Equal(
            2,
            firstPage.TotalCount);

        Assert.Equal(
            2,
            firstPage.TotalPages);

        var newestEntry =
            Assert.Single(firstPage.Items);

        Assert.Equal(
            secondTransaction.Id,
            newestEntry.TransactionId);

        Assert.Equal(
            secondPosting.Id,
            newestEntry.PostingId);

        Assert.Equal(
            "Deposit",
            newestEntry.TransactionType);

        Assert.Equal(
            "Credit",
            newestEntry.Direction);

        Assert.Equal(
            250.00m,
            newestEntry.Amount);

        Assert.Equal(
            currency,
            newestEntry.Currency);

        Assert.Equal(
            secondOccurredAtUtc,
            newestEntry.CreatedAtUtc);

        var secondPage =
            await queryService.GetLedgerEntriesAsync(
                ownerUserId,
                currency,
                page: 2,
                pageSize: 1,
                CancellationToken.None);

        Assert.NotNull(secondPage);

        var olderEntry =
            Assert.Single(secondPage.Items);

        Assert.Equal(
            firstTransaction.Id,
            olderEntry.TransactionId);

        Assert.Equal(
            100.00m,
            olderEntry.Amount);

        Assert.Equal(
            firstOccurredAtUtc,
            olderEntry.CreatedAtUtc);

        var otherUsersEntries =
            await queryService.GetLedgerEntriesAsync(
                otherUserId,
                currency,
                page: 1,
                pageSize: 20,
                CancellationToken.None);

        Assert.NotNull(otherUsersEntries);

        Assert.Equal(
            0,
            otherUsersEntries.TotalCount);

        Assert.Empty(
            otherUsersEntries.Items);

        var missingUsersWallet =
            await queryService.GetLedgerEntriesAsync(
                Guid.NewGuid(),
                currency,
                page: 1,
                pageSize: 20,
                CancellationToken.None);

        Assert.Null(missingUsersWallet);
    }
}