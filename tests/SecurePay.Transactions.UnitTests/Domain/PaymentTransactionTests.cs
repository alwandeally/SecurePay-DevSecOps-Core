using SecurePay.Transactions.Api.Domain.Entities;
using SecurePay.Transactions.Api.Domain.Enums;

using PaymentStatus =
    SecurePay.Transactions.Api.Domain.Enums.TransactionStatus;

namespace SecurePay.Transactions.UnitTests.Domain;

public sealed class PaymentTransactionTests
{
    [Fact]
    public void Create_WithValidValues_CreatesPendingTransaction()
    {
        var userId = Guid.NewGuid();

        var transaction = PaymentTransaction.Create(
            userId,
            TransactionType.Deposit,
            1250.50m,
            "zar",
            "deposit-001",
            "Initial account deposit");

        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.Equal(userId, transaction.UserId);
        Assert.Equal(TransactionType.Deposit, transaction.Type);
        Assert.Equal(1250.50m, transaction.Amount);
        Assert.Equal("ZAR", transaction.Currency);
        Assert.Equal("deposit-001", transaction.IdempotencyKey);
        Assert.Equal("Initial account deposit", transaction.Description);
        Assert.Equal(PaymentStatus.Pending, transaction.Status);
        Assert.StartsWith("TXN-", transaction.Reference);
        Assert.Null(transaction.CompletedAtUtc);
        Assert.Null(transaction.FailureReason);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            PaymentTransaction.Create(
                Guid.Empty,
                TransactionType.Deposit,
                500m,
                "ZAR",
                "deposit-002"));

        Assert.Equal("userId", exception.ParamName);
    }

    [Fact]
    public void Create_WithZeroAmount_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PaymentTransaction.Create(
                Guid.NewGuid(),
                TransactionType.Deposit,
                0m,
                "ZAR",
                "deposit-003"));
    }

    [Fact]
    public void Create_WithMoreThanTwoDecimalPlaces_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            PaymentTransaction.Create(
                Guid.NewGuid(),
                TransactionType.Deposit,
                100.123m,
                "ZAR",
                "deposit-004"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ZA")]
    [InlineData("12$")]
    public void Create_WithInvalidCurrency_ThrowsArgumentException(
        string currency)
    {
        Assert.Throws<ArgumentException>(() =>
            PaymentTransaction.Create(
                Guid.NewGuid(),
                TransactionType.Deposit,
                100m,
                currency,
                "deposit-005"));
    }

    [Fact]
    public void Create_WithBlankIdempotencyKey_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            PaymentTransaction.Create(
                Guid.NewGuid(),
                TransactionType.Deposit,
                100m,
                "ZAR",
                " "));
    }

    [Fact]
    public void MarkCompleted_WhenPending_CompletesTransaction()
    {
        var transaction = CreateValidTransaction();
        var completedAtUtc = DateTimeOffset.UtcNow;

        transaction.MarkCompleted(completedAtUtc);

        Assert.Equal(PaymentStatus.Completed, transaction.Status);
        Assert.Equal(completedAtUtc, transaction.CompletedAtUtc);
        Assert.Equal(completedAtUtc, transaction.UpdatedAtUtc);
        Assert.Null(transaction.FailureReason);
    }

    [Fact]
    public void MarkFailed_WhenPending_FailsTransaction()
    {
        var transaction = CreateValidTransaction();
        var failedAtUtc = DateTimeOffset.UtcNow;

        transaction.MarkFailed(
            "Insufficient funds",
            failedAtUtc);

        Assert.Equal(PaymentStatus.Failed, transaction.Status);
        Assert.Equal("Insufficient funds", transaction.FailureReason);
        Assert.Equal(failedAtUtc, transaction.UpdatedAtUtc);
        Assert.Null(transaction.CompletedAtUtc);
    }

    [Fact]
    public void MarkFailed_WhenAlreadyCompleted_ThrowsInvalidOperationException()
    {
        var transaction = CreateValidTransaction();

        transaction.MarkCompleted(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            transaction.MarkFailed(
                "This change should be rejected",
                DateTimeOffset.UtcNow));
    }

    private static PaymentTransaction CreateValidTransaction()
    {
        return PaymentTransaction.Create(
            Guid.NewGuid(),
            TransactionType.Deposit,
            500m,
            "ZAR",
            $"test-{Guid.NewGuid():N}");
    }
}