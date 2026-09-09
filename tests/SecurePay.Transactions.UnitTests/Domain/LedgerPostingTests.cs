using SecurePay.Transactions.Api.Domain.Entities;
using SecurePay.Transactions.Api.Domain.Enums;
using SecurePay.Transactions.Api.Domain.ValueObjects;

namespace SecurePay.Transactions.UnitTests.Domain;

public sealed class LedgerPostingTests
{
    [Fact]
    public void Create_WithBalancedLines_CreatesPostingAndEntries()
    {
        var paymentTransactionId = Guid.NewGuid();
        var debitAccountId = Guid.NewGuid();
        var creditAccountId = Guid.NewGuid();

        var occurredAtUtc = new DateTimeOffset(
            2026,
            9,
            9,
            14,
            30,
            0,
            TimeSpan.FromHours(2));

        var posting = LedgerPosting.Create(
            paymentTransactionId,
            " zar ",
            [
                new LedgerLine(
                    debitAccountId,
                    LedgerEntryDirection.Debit,
                    125.50m),
                new LedgerLine(
                    creditAccountId,
                    LedgerEntryDirection.Credit,
                    125.50m)
            ],
            occurredAtUtc);

        Assert.NotEqual(Guid.Empty, posting.Id);
        Assert.Equal(
            paymentTransactionId,
            posting.PaymentTransactionId);
        Assert.Equal("ZAR", posting.Currency);
        Assert.Equal(
            occurredAtUtc.ToUniversalTime(),
            posting.CreatedAtUtc);

        Assert.Collection(
            posting.Entries,
            debitEntry =>
            {
                Assert.NotEqual(
                    Guid.Empty,
                    debitEntry.Id);
                Assert.Equal(
                    posting.Id,
                    debitEntry.LedgerPostingId);
                Assert.Equal(
                    debitAccountId,
                    debitEntry.LedgerAccountId);
                Assert.Equal(
                    LedgerEntryDirection.Debit,
                    debitEntry.Direction);
                Assert.Equal(
                    125.50m,
                    debitEntry.Amount);
                Assert.Equal(
                    posting.CreatedAtUtc,
                    debitEntry.CreatedAtUtc);
            },
            creditEntry =>
            {
                Assert.NotEqual(
                    Guid.Empty,
                    creditEntry.Id);
                Assert.Equal(
                    posting.Id,
                    creditEntry.LedgerPostingId);
                Assert.Equal(
                    creditAccountId,
                    creditEntry.LedgerAccountId);
                Assert.Equal(
                    LedgerEntryDirection.Credit,
                    creditEntry.Direction);
                Assert.Equal(
                    125.50m,
                    creditEntry.Amount);
                Assert.Equal(
                    posting.CreatedAtUtc,
                    creditEntry.CreatedAtUtc);
            });
    }

    [Fact]
    public void Create_WithMultipleCreditLines_CreatesBalancedPosting()
    {
        var posting = LedgerPosting.Create(
            Guid.NewGuid(),
            "USD",
            [
                new LedgerLine(
                    Guid.NewGuid(),
                    LedgerEntryDirection.Debit,
                    100m),
                new LedgerLine(
                    Guid.NewGuid(),
                    LedgerEntryDirection.Credit,
                    75m),
                new LedgerLine(
                    Guid.NewGuid(),
                    LedgerEntryDirection.Credit,
                    25m)
            ],
            DateTimeOffset.UtcNow);

        var debitTotal =
            posting.Entries
                .Where(entry =>
                    entry.Direction ==
                    LedgerEntryDirection.Debit)
                .Sum(entry => entry.Amount);

        var creditTotal =
            posting.Entries
                .Where(entry =>
                    entry.Direction ==
                    LedgerEntryDirection.Credit)
                .Sum(entry => entry.Amount);

        Assert.Equal(3, posting.Entries.Count);
        Assert.Equal(100m, debitTotal);
        Assert.Equal(debitTotal, creditTotal);
    }

    [Fact]
    public void Create_WithEmptyPaymentTransactionId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => LedgerPosting.Create(
                Guid.Empty,
                "USD",
                CreateBalancedLines(),
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_WithNullLines_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => LedgerPosting.Create(
                Guid.NewGuid(),
                "USD",
                null!,
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_WithFewerThanTwoLines_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => LedgerPosting.Create(
                Guid.NewGuid(),
                "USD",
                [
                    new LedgerLine(
                        Guid.NewGuid(),
                        LedgerEntryDirection.Debit,
                        100m)
                ],
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_WithBlankCurrency_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => LedgerPosting.Create(
                Guid.NewGuid(),
                "   ",
                CreateBalancedLines(),
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_WithDefaultOccurrenceTime_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => LedgerPosting.Create(
                Guid.NewGuid(),
                "USD",
                CreateBalancedLines(),
                default));
    }

    [Fact]
    public void Create_WithMissingLedgerAccountId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => LedgerPosting.Create(
                Guid.NewGuid(),
                "USD",
                [
                    new LedgerLine(
                        Guid.Empty,
                        LedgerEntryDirection.Debit,
                        100m),
                    new LedgerLine(
                        Guid.NewGuid(),
                        LedgerEntryDirection.Credit,
                        100m)
                ],
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_WithInvalidDirection_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => LedgerPosting.Create(
                Guid.NewGuid(),
                "USD",
                [
                    new LedgerLine(
                        Guid.NewGuid(),
                        (LedgerEntryDirection)999,
                        100m),
                    new LedgerLine(
                        Guid.NewGuid(),
                        LedgerEntryDirection.Credit,
                        100m)
                ],
                DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveAmount_ThrowsArgumentOutOfRangeException(
        int amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => LedgerPosting.Create(
                Guid.NewGuid(),
                "USD",
                [
                    new LedgerLine(
                        Guid.NewGuid(),
                        LedgerEntryDirection.Debit,
                        amount),
                    new LedgerLine(
                        Guid.NewGuid(),
                        LedgerEntryDirection.Credit,
                        100m)
                ],
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_WithMoreThanTwoDecimalPlaces_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => LedgerPosting.Create(
                Guid.NewGuid(),
                "USD",
                [
                    new LedgerLine(
                        Guid.NewGuid(),
                        LedgerEntryDirection.Debit,
                        10.001m),
                    new LedgerLine(
                        Guid.NewGuid(),
                        LedgerEntryDirection.Credit,
                        10.001m)
                ],
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_WithAmountAboveMaximum_ThrowsArgumentOutOfRangeException()
    {
        var invalidAmount =
            LedgerPosting.MaximumEntryAmount + 0.01m;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => LedgerPosting.Create(
                Guid.NewGuid(),
                "USD",
                [
                    new LedgerLine(
                        Guid.NewGuid(),
                        LedgerEntryDirection.Debit,
                        invalidAmount),
                    new LedgerLine(
                        Guid.NewGuid(),
                        LedgerEntryDirection.Credit,
                        invalidAmount)
                ],
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_WithDuplicateAccount_ThrowsInvalidOperationException()
    {
        var accountId = Guid.NewGuid();

        Assert.Throws<InvalidOperationException>(
            () => LedgerPosting.Create(
                Guid.NewGuid(),
                "USD",
                [
                    new LedgerLine(
                        accountId,
                        LedgerEntryDirection.Debit,
                        100m),
                    new LedgerLine(
                        accountId,
                        LedgerEntryDirection.Credit,
                        100m)
                ],
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_WithUnequalTotals_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => LedgerPosting.Create(
                Guid.NewGuid(),
                "USD",
                [
                    new LedgerLine(
                        Guid.NewGuid(),
                        LedgerEntryDirection.Debit,
                        100m),
                    new LedgerLine(
                        Guid.NewGuid(),
                        LedgerEntryDirection.Credit,
                        99m)
                ],
                DateTimeOffset.UtcNow));
    }

    private static LedgerLine[] CreateBalancedLines()
    {
        return
        [
            new LedgerLine(
                Guid.NewGuid(),
                LedgerEntryDirection.Debit,
                100m),
            new LedgerLine(
                Guid.NewGuid(),
                LedgerEntryDirection.Credit,
                100m)
        ];
    }
}