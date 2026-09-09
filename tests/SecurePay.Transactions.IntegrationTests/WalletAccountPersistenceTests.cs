using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecurePay.Transactions.Api.Domain.Entities;
using SecurePay.Transactions.Api.Persistence;
using SecurePay.Transactions.IntegrationTests.Infrastructure;

namespace SecurePay.Transactions.IntegrationTests;

[Collection(TransactionsApiCollection.Name)]
public sealed class WalletAccountPersistenceTests(
    TransactionsApiFixture fixture)
{
    [Fact]
    public async Task WalletAccount_WhenSaved_CanBeReloaded()
    {
        var userId = Guid.NewGuid();
        var wallet = WalletAccount.Create(
            userId,
            " zar ");

        wallet.Credit(
            250.75m,
            DateTimeOffset.UtcNow);

        await using (var creationScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var creationDbContext =
                creationScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            creationDbContext.WalletAccounts.Add(wallet);

            await creationDbContext.SaveChangesAsync();
        }

        await using var verificationScope =
            fixture.Factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        var persistedWallet =
            await verificationDbContext.WalletAccounts
                .AsNoTracking()
                .SingleAsync(existing =>
                    existing.Id == wallet.Id);

        Assert.Equal(wallet.Id, persistedWallet.Id);
        Assert.Equal(userId, persistedWallet.UserId);
        Assert.Equal("ZAR", persistedWallet.Currency);
        Assert.Equal(250.75m, persistedWallet.Balance);
        Assert.NotEqual(0u, persistedWallet.Version);
        Assert.NotEqual(
            default,
            persistedWallet.CreatedAtUtc);
        Assert.NotNull(persistedWallet.UpdatedAtUtc);
    }

    [Fact]
    public async Task WalletAccount_WhenUserAndCurrencyAreDuplicated_RejectsSecondWallet()
    {
        var userId = Guid.NewGuid();

        var firstWallet = WalletAccount.Create(
            userId,
            "USD");

        await using (var creationScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var creationDbContext =
                creationScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            creationDbContext.WalletAccounts.Add(firstWallet);

            await creationDbContext.SaveChangesAsync();
        }

        var duplicateWallet = WalletAccount.Create(
            userId,
            "usd");

        await using (var duplicateScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var duplicateDbContext =
                duplicateScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            duplicateDbContext.WalletAccounts.Add(
                duplicateWallet);

            await Assert.ThrowsAsync<DbUpdateException>(
                () => duplicateDbContext.SaveChangesAsync());
        }

        await using var verificationScope =
            fixture.Factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        var persistedWalletCount =
            await verificationDbContext.WalletAccounts
                .AsNoTracking()
                .CountAsync(existing =>
                    existing.UserId == userId &&
                    existing.Currency == "USD");

        Assert.Equal(1, persistedWalletCount);
    }

    [Fact]
    public async Task WalletAccount_WhenVersionIsStale_RejectsSecondUpdate()
    {
        var wallet = WalletAccount.Create(
            Guid.NewGuid(),
            "EUR");

        wallet.Credit(
            100m,
            DateTimeOffset.UtcNow);

        await using (var seedScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var seedDbContext =
                seedScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            seedDbContext.WalletAccounts.Add(wallet);

            await seedDbContext.SaveChangesAsync();
        }

        await using var staleScope =
            fixture.Factory.Services.CreateAsyncScope();

        await using var winningScope =
            fixture.Factory.Services.CreateAsyncScope();

        var staleDbContext =
            staleScope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        var winningDbContext =
            winningScope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        var staleWallet =
            await staleDbContext.WalletAccounts
                .SingleAsync(existing =>
                    existing.Id == wallet.Id);

        var winningWallet =
            await winningDbContext.WalletAccounts
                .SingleAsync(existing =>
                    existing.Id == wallet.Id);

        winningWallet.Credit(
            25m,
            DateTimeOffset.UtcNow);

        await winningDbContext.SaveChangesAsync();

        staleWallet.Debit(
            10m,
            DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => staleDbContext.SaveChangesAsync());

        await using var verificationScope =
            fixture.Factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        var persistedWallet =
            await verificationDbContext.WalletAccounts
                .AsNoTracking()
                .SingleAsync(existing =>
                    existing.Id == wallet.Id);

        Assert.Equal(125m, persistedWallet.Balance);
    }
}