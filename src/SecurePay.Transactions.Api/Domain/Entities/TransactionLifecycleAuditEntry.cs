using SecurePay.Transactions.Api.Domain.Enums;

namespace SecurePay.Transactions.Api.Domain.Entities;

public sealed class TransactionLifecycleAuditEntry
{
    private TransactionLifecycleAuditEntry()
    {
    }

    public Guid Id { get; private set; }

    public Guid TransactionId { get; private set; }

    public Guid OperatorId { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public string Action { get; private set; } = null!;

    public TransactionStatus ResultingStatus { get; private set; }

    public static TransactionLifecycleAuditEntry Create(
        Guid transactionId,
        Guid operatorId,
        DateTimeOffset occurredAt,
        TransactionStatus resultingStatus)
    {
        if (transactionId == Guid.Empty)
        {
            throw new ArgumentException(
                "A transaction ID is required.", nameof(transactionId));
        }

        if (operatorId == Guid.Empty)
        {
            throw new ArgumentException(
                "An operator ID is required.", nameof(operatorId));
        }

        var action = resultingStatus switch
        {
            TransactionStatus.Completed => "Complete",
            TransactionStatus.Failed => "Fail",
            _ => throw new ArgumentOutOfRangeException(nameof(resultingStatus))
        };

        return new TransactionLifecycleAuditEntry
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            OperatorId = operatorId,
            OccurredAtUtc = occurredAt.ToUniversalTime(),
            Action = action,
            ResultingStatus = resultingStatus
        };
    }
}
