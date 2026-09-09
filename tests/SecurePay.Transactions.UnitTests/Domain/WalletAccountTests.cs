using SecurePay.Transactions.Api.Domain.Entities;

namespace SecurePay.Transactions.UnitTests.Domain;

public sealed class WalletAccountTests
{
    [Fact]
    public void Create_WithValidValues_CreatesEmptyWallet()
    {
        var userId = Guid.NewGuid();
        var beforeCreation = DateTimeOffset.UtcNow;

        var wallet = WalletAccount.Create(
            userId,
            " usd ");

        var afterCreation = DateTimeOffset.UtcNow;

        Assert.NotEqual(Guid.Empty, wallet.Id);
        Assert.Equal(userId, wallet.UserId);
        Assert.Equal("USD", wallet.Currency);
        Assert.Equal(0m, wallet.Balance);
        Assert.InRange(
            wallet.CreatedAtUtc,
            beforeCreation,
            afterCreation);
        Assert.Null(wallet.UpdatedAtUtc);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => WalletAccount.Create(
                Guid.Empty,
                "USD"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("US1")]
    public void Create_WithInvalidCurrency_ThrowsArgumentException(
        string currency)
    {
        Assert.Throws<ArgumentException>(
            () => WalletAccount.Create(
                Guid.NewGuid(),
                currency));
    }

    [Fact]
    public void Credit_WithValidAmount_IncreasesBalance()
    {
        var wallet = CreateWallet();
        var occurredAtUtc = DateTimeOffset.UtcNow;

        wallet.Credit(
            125.50m,
            occurredAtUtc);

        Assert.Equal(125.50m, wallet.Balance);
        Assert.Equal(occurredAtUtc, wallet.UpdatedAtUtc);
    }

    [Fact]
    public void Debit_WithAvailableFunds_DecreasesBalance()
    {
        var wallet = CreateWallet();
        wallet.Credit(
            100m,
            DateTimeOffset.UtcNow);

        var occurredAtUtc = DateTimeOffset.UtcNow;

        wallet.Debit(
            40m,
            occurredAtUtc);

        Assert.Equal(60m, wallet.Balance);
        Assert.Equal(occurredAtUtc, wallet.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Credit_WithNonPositiveAmount_DoesNotChangeBalance(
        int amount)
    {
        var wallet = CreateWallet();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => wallet.Credit(
                amount,
                DateTimeOffset.UtcNow));

        Assert.Equal(0m, wallet.Balance);
        Assert.Null(wallet.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Debit_WithNonPositiveAmount_DoesNotChangeBalance(
        int amount)
    {
        var wallet = CreateWallet();
        wallet.Credit(
            100m,
            DateTimeOffset.UtcNow);

        var originalUpdatedAtUtc = wallet.UpdatedAtUtc;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => wallet.Debit(
                amount,
                DateTimeOffset.UtcNow));

        Assert.Equal(100m, wallet.Balance);
        Assert.Equal(
            originalUpdatedAtUtc,
            wallet.UpdatedAtUtc);
    }

    [Fact]
    public void Credit_WithMoreThanTwoDecimalPlaces_DoesNotChangeBalance()
    {
        var wallet = CreateWallet();

        Assert.Throws<ArgumentException>(
            () => wallet.Credit(
                0.001m,
                DateTimeOffset.UtcNow));

        Assert.Equal(0m, wallet.Balance);
        Assert.Null(wallet.UpdatedAtUtc);
    }

    [Fact]
    public void Debit_WithInsufficientFunds_DoesNotChangeBalance()
    {
        var wallet = CreateWallet();
        wallet.Credit(
            50m,
            DateTimeOffset.UtcNow);

        var originalUpdatedAtUtc = wallet.UpdatedAtUtc;

        Assert.Throws<InvalidOperationException>(
            () => wallet.Debit(
                50.01m,
                DateTimeOffset.UtcNow));

        Assert.Equal(50m, wallet.Balance);
        Assert.Equal(
            originalUpdatedAtUtc,
            wallet.UpdatedAtUtc);
    }

    [Fact]
    public void Credit_AboveMaximumBalance_DoesNotChangeBalance()
    {
        var wallet = CreateWallet();
        var originalUpdatedAtUtc = DateTimeOffset.UtcNow;

        wallet.Credit(
            WalletAccount.MaximumBalance,
            originalUpdatedAtUtc);

        Assert.Throws<InvalidOperationException>(
            () => wallet.Credit(
                0.01m,
                DateTimeOffset.UtcNow));

        Assert.Equal(
            WalletAccount.MaximumBalance,
            wallet.Balance);
        Assert.Equal(
            originalUpdatedAtUtc,
            wallet.UpdatedAtUtc);
    }

    private static WalletAccount CreateWallet()
    {
        return WalletAccount.Create(
            Guid.NewGuid(),
            "USD");
    }
}