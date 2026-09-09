using SecurePay.Transactions.Api.Domain.Enums;

namespace SecurePay.Transactions.Api.Domain.Entities;

public sealed class LedgerEntry
{
    private LedgerEntry()
    {
    }

    private LedgerEntry(
        Guid ledgerPostingId,
        Guid ledgerAccountId,
        LedgerEntryDirection direction,
        decimal amount,
        DateTimeOffset occurredAtUtc)
    {
        Id = Guid.NewGuid();
        LedgerPostingId = ledgerPostingId;
        LedgerAccountId = ledgerAccountId;
        Direction = direction;
        Amount = amount;
        CreatedAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid LedgerPostingId { get; private set; }

    public Guid LedgerAccountId { get; private set; }

    public LedgerEntryDirection Direction { get; private set; }

    public decimal Amount { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    internal static LedgerEntry Create(
        Guid ledgerPostingId,
        Guid ledgerAccountId,
        LedgerEntryDirection direction,
        decimal amount,
        DateTimeOffset occurredAtUtc)
    {
        return new LedgerEntry(
            ledgerPostingId,
            ledgerAccountId,
            direction,
            amount,
            occurredAtUtc);
    }
}