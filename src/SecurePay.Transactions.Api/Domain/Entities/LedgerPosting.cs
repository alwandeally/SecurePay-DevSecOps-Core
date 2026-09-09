using SecurePay.Transactions.Api.Domain.Enums;
using SecurePay.Transactions.Api.Domain.ValueObjects;

namespace SecurePay.Transactions.Api.Domain.Entities;

public sealed class LedgerPosting
{
    public const decimal MaximumEntryAmount =
        9_999_999_999_999_999.99m;

    private readonly List<LedgerEntry> _entries = [];

    private LedgerPosting()
    {
    }

    public Guid Id { get; private set; }

    public Guid PaymentTransactionId { get; private set; }

    public string Currency { get; private set; } =
        string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<LedgerEntry> Entries =>
        _entries.AsReadOnly();

    public static LedgerPosting Create(
        Guid paymentTransactionId,
        string currency,
        IReadOnlyCollection<LedgerLine> lines,
        DateTimeOffset occurredAtUtc)
    {
        if (paymentTransactionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Payment transaction ID is required.",
                nameof(paymentTransactionId));
        }

        ArgumentNullException.ThrowIfNull(lines);

        if (lines.Count < 2)
        {
            throw new ArgumentException(
                "A ledger posting requires at least two entries.",
                nameof(lines));
        }

        if (occurredAtUtc == default)
        {
            throw new ArgumentException(
                "The occurrence time is required.",
                nameof(occurredAtUtc));
        }

        var normalizedCurrency =
            NormalizeCurrency(currency);

        var accountIds = new HashSet<Guid>();
        var debitTotal = 0m;
        var creditTotal = 0m;

        foreach (var line in lines)
        {
            ValidateLine(line);

            if (!accountIds.Add(line.LedgerAccountId))
            {
                throw new InvalidOperationException(
                    "A ledger account cannot appear more than once in a posting.");
            }

            if (line.Direction ==
                LedgerEntryDirection.Debit)
            {
                debitTotal += line.Amount;
            }
            else
            {
                creditTotal += line.Amount;
            }
        }

        if (debitTotal == 0m ||
            creditTotal == 0m ||
            debitTotal != creditTotal)
        {
            throw new InvalidOperationException(
                "Ledger debit and credit totals must be equal and greater than zero.");
        }

        var posting = new LedgerPosting
        {
            Id = Guid.NewGuid(),
            PaymentTransactionId = paymentTransactionId,
            Currency = normalizedCurrency,
            CreatedAtUtc =
                occurredAtUtc.ToUniversalTime()
        };

        foreach (var line in lines)
        {
            posting._entries.Add(
                LedgerEntry.Create(
                    posting.Id,
                    line.LedgerAccountId,
                    line.Direction,
                    line.Amount,
                    posting.CreatedAtUtc));
        }

        return posting;
    }

    private static void ValidateLine(
        LedgerLine line)
    {
        if (line.LedgerAccountId == Guid.Empty)
        {
            throw new ArgumentException(
                "Every ledger entry requires an account ID.",
                nameof(line));
        }

        if (!Enum.IsDefined(line.Direction))
        {
            throw new ArgumentOutOfRangeException(
                nameof(line),
                "Every ledger entry requires a valid direction.");
        }

        if (line.Amount <= 0m ||
            line.Amount > MaximumEntryAmount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(line),
                $"Every ledger amount must be between 0.01 and {MaximumEntryAmount}.");
        }

        if (decimal.Round(line.Amount, 2) !=
            line.Amount)
        {
            throw new ArgumentException(
                "Ledger amounts cannot have more than two decimal places.",
                nameof(line));
        }
    }

    private static string NormalizeCurrency(
        string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException(
                "Currency is required.",
                nameof(currency));
        }

        var normalizedCurrency =
            currency.Trim().ToUpperInvariant();

        if (normalizedCurrency.Length != 3 ||
            !normalizedCurrency.All(char.IsLetter))
        {
            throw new ArgumentException(
                "Currency must be a valid three-letter code.",
                nameof(currency));
        }

        return normalizedCurrency;
    }
}