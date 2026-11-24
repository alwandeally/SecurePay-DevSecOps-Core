using Microsoft.EntityFrameworkCore;
using SecurePay.Transactions.Api.Application.Operations;
using SecurePay.Transactions.Api.Contracts.Operations;
using SecurePay.Transactions.Api.Domain.Entities;
using SecurePay.Transactions.Api.Domain.Enums;
using SecurePay.Transactions.Api.Persistence;

namespace SecurePay.Transactions.UnitTests.Application.Operations;

public sealed class OperationsTransactionQueryServiceTests
{
    [Fact]
    public async Task QueryAsync_FiltersByStatusTypeAndUser()
    {
        await using var dbContext = CreateDbContext();

        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var completedAtUtc = DateTimeOffset.UtcNow;

        var expected = CreateTransaction(
            targetUserId,
            TransactionType.Deposit,
            "expected");

        expected.MarkCompleted(completedAtUtc);

        var wrongType = CreateTransaction(
            targetUserId,
            TransactionType.Withdrawal,
            "wrong-type");

        wrongType.MarkCompleted(completedAtUtc);

        var wrongUser = CreateTransaction(
            otherUserId,
            TransactionType.Deposit,
            "wrong-user");

        wrongUser.MarkCompleted(completedAtUtc);

        var pending = CreateTransaction(
            targetUserId,
            TransactionType.Deposit,
            "pending");

        dbContext.PaymentTransactions.AddRange(
            expected,
            wrongType,
            wrongUser,
            pending);

        await dbContext.SaveChangesAsync();

        var service =
            new OperationsTransactionQueryService(dbContext);

        var result = await service.QueryAsync(
            new OperationsTransactionQuery
            {
                Status = "completed",
                TransactionType = "deposit",
                UserId = targetUserId
            },
            CancellationToken.None);

        var item = Assert.Single(result.Items);

        Assert.Equal(expected.Id, item.TransactionId);
        Assert.Equal("Completed", item.Status);
        Assert.Equal("Deposit", item.TransactionType);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task QueryAsync_ReturnsRequestedPageAndTotals()
    {
        await using var dbContext = CreateDbContext();

        var userId = Guid.NewGuid();
        var initialCreatedAtUtc =
            DateTimeOffset.UtcNow.AddMinutes(-10);

        var transactions =
            Enumerable.Range(0, 5)
                .Select(index =>
                    CreateTransaction(
                        userId,
                        TransactionType.Deposit,
                        $"pagination-{index}"))
                .ToArray();

        for (var index = 0;
             index < transactions.Length;
             index++)
        {
            var transaction = transactions[index];

            dbContext.PaymentTransactions.Add(transaction);

            dbContext.Entry(transaction)
                .Property(item => item.CreatedAtUtc)
                .CurrentValue =
                    initialCreatedAtUtc.AddMinutes(index);
        }

        await dbContext.SaveChangesAsync();

        var service =
            new OperationsTransactionQueryService(dbContext);

        var result = await service.QueryAsync(
            new OperationsTransactionQuery
            {
                Page = 2,
                PageSize = 2
            },
            CancellationToken.None);

        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);

        Assert.Equal(
            new[]
            {
                transactions[2].Id,
                transactions[1].Id
            },
            result.Items
                .Select(item => item.TransactionId)
                .ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task QueryAsync_RejectsInvalidPageSize(
        int pageSize)
    {
        await using var dbContext = CreateDbContext();

        var service =
            new OperationsTransactionQueryService(dbContext);

        await Assert.ThrowsAsync<
            ArgumentOutOfRangeException>(
            async () =>
            {
                await service.QueryAsync(
                    new OperationsTransactionQuery
                    {
                        PageSize = pageSize
                    },
                    CancellationToken.None);
            });
    }

    [Fact]
    public async Task QueryAsync_RejectsInvalidStatus()
    {
        await using var dbContext = CreateDbContext();

        var service =
            new OperationsTransactionQueryService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(
            async () =>
            {
                await service.QueryAsync(
                    new OperationsTransactionQuery
                    {
                        Status = "Approved"
                    },
                    CancellationToken.None);
            });
    }

    [Fact]
    public async Task QueryAsync_RejectsReversedDateRange()
    {
        await using var dbContext = CreateDbContext();

        var service =
            new OperationsTransactionQueryService(dbContext);

        var createdFromUtc = DateTimeOffset.UtcNow;

        await Assert.ThrowsAsync<ArgumentException>(
            async () =>
            {
                await service.QueryAsync(
                    new OperationsTransactionQuery
                    {
                        CreatedFromUtc = createdFromUtc,
                        CreatedToUtc =
                            createdFromUtc.AddMinutes(-1)
                    },
                    CancellationToken.None);
            });
    }

    private static PaymentTransaction CreateTransaction(
        Guid userId,
        TransactionType transactionType,
        string idempotencyKey)
    {
        return PaymentTransaction.Create(
            userId,
            transactionType,
            100m,
            "ZAR",
            idempotencyKey);
    }

    private static TransactionsDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<TransactionsDbContext>()
                .UseInMemoryDatabase(
                    $"operations-query-{Guid.NewGuid()}")
                .Options;

        return new TransactionsDbContext(options);
    }
}
