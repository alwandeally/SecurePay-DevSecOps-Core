using SecurePay.Transactions.Api.Domain.Entities;
using SecurePay.Transactions.Api.Domain.Enums;

namespace SecurePay.Transactions.UnitTests.Domain;

public sealed class LedgerAccountTests
{
    [Fact]
    public void CreateCustomerWallet_WithValidValues_CreatesAccount()
    {
        var walletId = Guid.NewGuid();
        var beforeCreation = DateTimeOffset.UtcNow;

        var account = LedgerAccount.CreateCustomerWallet(
            walletId,
            " zar ");

        var afterCreation = DateTimeOffset.UtcNow;

        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Equal(
            LedgerAccountType.CustomerWallet,
            account.AccountType);
        Assert.Equal(walletId, account.WalletId);
        Assert.Equal("ZAR", account.Currency);
        Assert.InRange(
            account.CreatedAtUtc,
            beforeCreation,
            afterCreation);
    }

    [Fact]
    public void CreateCustomerWallet_WithEmptyWalletId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => LedgerAccount.CreateCustomerWallet(
                Guid.Empty,
                "USD"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("US1")]
    public void CreateCustomerWallet_WithInvalidCurrency_ThrowsArgumentException(
        string currency)
    {
        Assert.Throws<ArgumentException>(
            () => LedgerAccount.CreateCustomerWallet(
                Guid.NewGuid(),
                currency));
    }

    [Fact]
    public void CreatePlatformClearing_WithValidCurrency_CreatesAccount()
    {
        var beforeCreation = DateTimeOffset.UtcNow;

        var account =
            LedgerAccount.CreatePlatformClearing(
                " eur ");

        var afterCreation = DateTimeOffset.UtcNow;

        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Equal(
            LedgerAccountType.PlatformClearing,
            account.AccountType);
        Assert.Null(account.WalletId);
        Assert.Equal("EUR", account.Currency);
        Assert.InRange(
            account.CreatedAtUtc,
            beforeCreation,
            afterCreation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    public void CreatePlatformClearing_WithInvalidCurrency_ThrowsArgumentException(
        string currency)
    {
        Assert.Throws<ArgumentException>(
            () => LedgerAccount.CreatePlatformClearing(
                currency));
    }
}