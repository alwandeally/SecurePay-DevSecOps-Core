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
                TransactionType.Transfer,
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

            seedDbContext.PaymentTransactions.Add(transaction);

            await seedDbContext.SaveChangesAsync();
        }

        await using var staleScope =
            fixture.Factory.Services.CreateAsyncScope();

        var staleDbContext =
            staleScope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        var staleTransaction =
            await staleDbContext.PaymentTransactions
                .SingleAsync(existing =>
                    existing.Id == transaction.Id);

        Assert.Equal(
            PaymentStatus.Pending,
            staleTransaction.Status);

        await using (var winningScope =
            fixture.Factory.Services.CreateAsyncScope())
        {
            var winningService =
                winningScope.ServiceProvider
                    .GetRequiredService<ITransactionService>();

            var completed =
                await winningService.CompleteAsync(
                    transaction.Id,
                    CancellationToken.None);

            Assert.NotNull(completed);
            Assert.Equal(
                nameof(PaymentStatus.Completed),
                completed.Status);
        }

        var staleService =
            staleScope.ServiceProvider
                .GetRequiredService<ITransactionService>();

        var exception =
            await Assert.ThrowsAsync<
                TransactionConcurrencyException>(
                () => staleService.FailAsync(
                    transaction.Id,
                    "A stale operation attempted to fail the transaction.",
                    CancellationToken.None));

        Assert.IsType<DbUpdateConcurrencyException>(
            exception.InnerException);

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
            PaymentStatus.Completed,
            persistedTransaction.Status);

        Assert.NotNull(
            persistedTransaction.CompletedAtUtc);

        Assert.Null(
            persistedTransaction.FailureReason);
    }
}