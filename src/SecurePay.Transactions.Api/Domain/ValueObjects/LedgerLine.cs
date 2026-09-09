using SecurePay.Transactions.Api.Domain.Enums;

namespace SecurePay.Transactions.Api.Domain.ValueObjects;

public readonly record struct LedgerLine(
    Guid LedgerAccountId,
    LedgerEntryDirection Direction,
    decimal Amount);