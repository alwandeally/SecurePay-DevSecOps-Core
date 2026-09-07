using Microsoft.EntityFrameworkCore;
using SecurePay.Transactions.Api.Application.Transactions;
using SecurePay.Transactions.Api.Domain.Entities;
using SecurePay.Transactions.Api.Domain.Enums;
using SecurePay.Transactions.Api.Persistence;

namespace SecurePay.Transactions.UnitTests.Application.Transactions;

public sealed class TransactionServiceTests
{
    [Fact]
    public async Task GetByIdAsync_WhenOwned_ReturnsMappedTransaction()
    {
        await using var dbContext = CreateDbContext();

        var userId = Guid.NewGuid();
        var transaction = CreateTransaction(userId, 1250m);

        dbContext.PaymentTransactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        var service = new TransactionService(dbContext);

        var response = await service.GetByIdAsync(
            userId,
            transaction.Id,
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(transaction.Id, response.TransactionId);
        Assert.Equal(userId, response.UserId);
        Assert.Equal(1250m, response.Amount);
        Assert.Equal("ZAR", response.Currency);
        Assert.Equal("Deposit", response.TransactionType);
        Assert.Equal("Pending", response.Status);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUnknown_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();

        var service = new TransactionService(dbContext);

        var response = await service.GetByIdAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Null(response);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOwnedByAnotherUser_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();

        var ownerId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();
        var transaction = CreateTransaction(ownerId, 500m);

        dbContext.PaymentTransactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        var service = new TransactionService(dbContext);

        var response = await service.GetByIdAsync(
            requestingUserId,
            transaction.Id,
            CancellationToken.None);

        Assert.Null(response);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyAuthenticatedUsersTransactions()
    {
        await using var dbContext = CreateDbContext();

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var firstOwned = CreateTransaction(userId, 100m);
        var secondOwned = CreateTransaction(userId, 200m);
        var otherUsers = CreateTransaction(otherUserId, 300m);

        dbContext.PaymentTransactions.AddRange(
            firstOwned,
            secondOwned,
            otherUsers);

        await dbContext.SaveChangesAsync();

        var service = new TransactionService(dbContext);

        var responses = await service.GetAllAsync(
            userId,
            CancellationToken.None);

        Assert.Equal(2, responses.Count);
        Assert.All(
            responses,
            response => Assert.Equal(userId, response.UserId));

        Assert.DoesNotContain(
            responses,
            response =>
                response.TransactionId == otherUsers.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsNewestTransactionsFirst()
    {
        await using var dbContext = CreateDbContext();

        var userId = Guid.NewGuid();
        var olderTransaction = CreateTransaction(userId, 100m);

        await Task.Delay(TimeSpan.FromMilliseconds(10));

        var newerTransaction = CreateTransaction(userId, 200m);

        dbContext.PaymentTransactions.AddRange(
            olderTransaction,
            newerTransaction);

        await dbContext.SaveChangesAsync();

        var service = new TransactionService(dbContext);

        var responses = await service.GetAllAsync(
            userId,
            CancellationToken.None);

        Assert.Collection(
            responses,
            first => Assert.Equal(
                newerTransaction.Id,
                first.TransactionId),
            second => Assert.Equal(
                olderTransaction.Id,
                second.TransactionId));
    }

    [Fact]
    public async Task GetAllAsync_WhenNoTransactions_ReturnsEmptyList()
    {
        await using var dbContext = CreateDbContext();

        var service = new TransactionService(dbContext);

        var responses = await service.GetAllAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Empty(responses);
    }

    private static TransactionsDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<TransactionsDbContext>()
                .UseInMemoryDatabase(
                    $"transactions-{Guid.NewGuid():N}")
                .Options;

        return new TransactionsDbContext(options);
    }

    private static PaymentTransaction CreateTransaction(
        Guid userId,
        decimal amount)
    {
        return PaymentTransaction.Create(
            userId,
            TransactionType.Deposit,
            amount,
            "ZAR",
            $"test-{Guid.NewGuid():N}",
            "Transaction service test");
    }
}
