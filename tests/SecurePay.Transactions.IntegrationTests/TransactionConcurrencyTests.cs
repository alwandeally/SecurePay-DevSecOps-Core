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
public sealed class TransactionConcurrencyTests(
    TransactionsApiFixture fixture)
{
    [Fact]
    public async Task FailAsync_WhenVersionIsStale_RejectsSecondUpdate()
    {
        var transaction =
            PaymentTransaction.Create(
                Guid.NewGuid(),
                TransactionType.Deposit,
                125.50m,
                "ZAR",
                $"concurrency-{Guid.NewGuid():N}",
                "Optimistic concurrency integration test");

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

        await using var staleScope =
            fixture.Factory.Services.CreateAsyncScope();

        var staleDbContext =
            staleScope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        _ = await staleDbContext.PaymentTransactions
            .SingleAsync(existing =>
                existing.Id == transaction.Id);

        await using (var winningScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var winningDbContext =
                winningScope.ServiceProvider
                    .GetRequiredService<TransactionsDbContext>();

            var winningService =
                new TransactionService(
                    winningDbContext);

            var winningResponse =
                await winningService.FailAsync(
                    transaction.Id,
                    "Processor rejected the transaction",
                    CancellationToken.None);

            Assert.NotNull(winningResponse);
            Assert.Equal(
                "Failed",
                winningResponse.Status);
        }

        var staleService =
            new TransactionService(
                staleDbContext);

        await Assert.ThrowsAsync<TransactionConcurrencyException>(
            () => staleService.FailAsync(
                transaction.Id,
                "Stale processor decision",
                CancellationToken.None));

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
            PaymentStatus.Failed,
            persistedTransaction.Status);

        Assert.Equal(
            "Processor rejected the transaction",
            persistedTransaction.FailureReason);
    }
}