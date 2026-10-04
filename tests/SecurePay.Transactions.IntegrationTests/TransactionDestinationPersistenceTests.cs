using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecurePay.Transactions.Api.Domain.Entities;
using SecurePay.Transactions.Api.Domain.Enums;
using SecurePay.Transactions.Api.Persistence;
using SecurePay.Transactions.IntegrationTests.Infrastructure;

namespace SecurePay.Transactions.IntegrationTests;

[Collection(TransactionsApiCollection.Name)]
public sealed class TransactionDestinationPersistenceTests(
    TransactionsApiFixture fixture)
{
    [Fact]
    public async Task Transfer_WhenSaved_PersistsDestinationUserId()
    {
        var sourceUserId = Guid.NewGuid();
        var destinationUserId = Guid.NewGuid();

        var transaction = PaymentTransaction.Create(
            sourceUserId,
            TransactionType.Transfer,
            125.50m,
            "zar",
            $"transfer-{Guid.NewGuid():N}",
            "Destination persistence test",
            destinationUserId);

        await using (var creationScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var creationDbContext =
                creationScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            creationDbContext.PaymentTransactions.Add(
                transaction);

            await creationDbContext.SaveChangesAsync();
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

        Assert.Equal(
            sourceUserId,
            persistedTransaction.UserId);

        Assert.Equal(
            destinationUserId,
            persistedTransaction.DestinationUserId);

        Assert.Equal(
            TransactionType.Transfer,
            persistedTransaction.Type);

        Assert.Equal(
            "ZAR",
            persistedTransaction.Currency);
    }

    [Fact]
    public async Task Transfer_WhenDestinationIsNull_DatabaseRejectsSave()
    {
        var transaction = PaymentTransaction.Create(
            Guid.NewGuid(),
            TransactionType.Transfer,
            50m,
            "USD",
            $"transfer-null-{Guid.NewGuid():N}",
            destinationUserId: Guid.NewGuid());

        await using var scope =
            fixture.Factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        dbContext.PaymentTransactions.Add(transaction);

        dbContext.Entry(transaction)
            .Property<Guid?>(
                nameof(PaymentTransaction.DestinationUserId))
            .CurrentValue = null;

        await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Transfer_WhenDestinationMatchesSource_DatabaseRejectsSave()
    {
        var sourceUserId = Guid.NewGuid();

        var transaction = PaymentTransaction.Create(
            sourceUserId,
            TransactionType.Transfer,
            75m,
            "EUR",
            $"transfer-self-{Guid.NewGuid():N}",
            destinationUserId: Guid.NewGuid());

        await using var scope =
            fixture.Factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        dbContext.PaymentTransactions.Add(transaction);

        dbContext.Entry(transaction)
            .Property<Guid?>(
                nameof(PaymentTransaction.DestinationUserId))
            .CurrentValue = sourceUserId;

        await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Deposit_WhenDestinationIsPresent_DatabaseRejectsSave()
    {
        var transaction = PaymentTransaction.Create(
            Guid.NewGuid(),
            TransactionType.Deposit,
            100m,
            "GBP",
            $"deposit-destination-{Guid.NewGuid():N}");

        await using var scope =
            fixture.Factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        dbContext.PaymentTransactions.Add(transaction);

        dbContext.Entry(transaction)
            .Property<Guid?>(
                nameof(PaymentTransaction.DestinationUserId))
            .CurrentValue = Guid.NewGuid();

        await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());
    }
}