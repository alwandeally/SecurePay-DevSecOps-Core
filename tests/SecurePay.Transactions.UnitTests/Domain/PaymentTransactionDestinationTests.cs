using SecurePay.Transactions.Api.Domain.Entities;
using SecurePay.Transactions.Api.Domain.Enums;

namespace SecurePay.Transactions.UnitTests.Domain;

public sealed class PaymentTransactionDestinationTests
{
    [Fact]
    public void Create_TransferWithDestination_CreatesTransaction()
    {
        var sourceUserId = Guid.NewGuid();
        var destinationUserId = Guid.NewGuid();

        var transaction = PaymentTransaction.Create(
            sourceUserId,
            TransactionType.Transfer,
            250m,
            "ZAR",
            "transfer-001",
            "Wallet transfer",
            destinationUserId);

        Assert.Equal(
            destinationUserId,
            transaction.DestinationUserId);
    }

    [Fact]
    public void Create_TransferWithoutDestination_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            PaymentTransaction.Create(
                Guid.NewGuid(),
                TransactionType.Transfer,
                250m,
                "ZAR",
                "transfer-002"));

        Assert.Equal(
            "destinationUserId",
            exception.ParamName);
    }

    [Fact]
    public void Create_TransferWithEmptyDestination_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            PaymentTransaction.Create(
                Guid.NewGuid(),
                TransactionType.Transfer,
                250m,
                "ZAR",
                "transfer-003",
                destinationUserId: Guid.Empty));

        Assert.Equal(
            "destinationUserId",
            exception.ParamName);
    }

    [Fact]
    public void Create_TransferToSameUser_ThrowsArgumentException()
    {
        var userId = Guid.NewGuid();

        var exception = Assert.Throws<ArgumentException>(() =>
            PaymentTransaction.Create(
                userId,
                TransactionType.Transfer,
                250m,
                "ZAR",
                "transfer-004",
                destinationUserId: userId));

        Assert.Equal(
            "destinationUserId",
            exception.ParamName);
    }

    [Theory]
    [InlineData(TransactionType.Deposit)]
    [InlineData(TransactionType.Withdrawal)]
    public void Create_NonTransferWithDestination_ThrowsArgumentException(
        TransactionType transactionType)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            PaymentTransaction.Create(
                Guid.NewGuid(),
                transactionType,
                250m,
                "ZAR",
                $"non-transfer-{Guid.NewGuid():N}",
                destinationUserId: Guid.NewGuid()));

        Assert.Equal(
            "destinationUserId",
            exception.ParamName);
    }
}