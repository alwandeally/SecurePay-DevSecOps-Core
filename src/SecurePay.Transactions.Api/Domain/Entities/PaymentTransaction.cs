using SecurePay.Transactions.Api.Domain.Enums;

using PaymentStatus =
    SecurePay.Transactions.Api.Domain.Enums.TransactionStatus;

namespace SecurePay.Transactions.Api.Domain.Entities;

public sealed class PaymentTransaction
{
    private PaymentTransaction()
    {
    }

    private PaymentTransaction(
        Guid userId,
        TransactionType type,
        decimal amount,
        string currency,
        string idempotencyKey,
        string? description)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Type = type;
        Amount = amount;
        Currency = currency;
        IdempotencyKey = idempotencyKey;
        Description = description;
        Reference = $"TXN-{Guid.NewGuid():N}".ToUpperInvariant();
        Status = PaymentStatus.Pending;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public TransactionType Type { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public string IdempotencyKey { get; private set; } = string.Empty;

    public string Reference { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public PaymentStatus Status { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public static PaymentTransaction Create(
        Guid userId,
        TransactionType type,
        decimal amount,
        string currency,
        string idempotencyKey,
        string? description = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID is required.",
                nameof(userId));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                "A valid transaction type is required.");
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Transaction amount must be greater than zero.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentException(
                "Transaction amount cannot have more than two decimal places.",
                nameof(amount));
        }

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

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException(
                "Idempotency key is required.",
                nameof(idempotencyKey));
        }

        var normalizedIdempotencyKey = idempotencyKey.Trim();

        if (normalizedIdempotencyKey.Length > 128)
        {
            throw new ArgumentException(
                "Idempotency key cannot exceed 128 characters.",
                nameof(idempotencyKey));
        }

        var normalizedDescription =
            string.IsNullOrWhiteSpace(description)
                ? null
                : description.Trim();

        if (normalizedDescription?.Length > 500)
        {
            throw new ArgumentException(
                "Description cannot exceed 500 characters.",
                nameof(description));
        }

        return new PaymentTransaction(
            userId,
            type,
            amount,
            normalizedCurrency,
            normalizedIdempotencyKey,
            normalizedDescription);
    }

    public void MarkCompleted(DateTimeOffset completedAtUtc)
    {
        EnsurePending();

        Status = PaymentStatus.Completed;
        CompletedAtUtc = completedAtUtc;
        UpdatedAtUtc = completedAtUtc;
        FailureReason = null;
    }

    public void MarkFailed(
        string failureReason,
        DateTimeOffset failedAtUtc)
    {
        EnsurePending();

        if (string.IsNullOrWhiteSpace(failureReason))
        {
            throw new ArgumentException(
                "Failure reason is required.",
                nameof(failureReason));
        }

        Status = PaymentStatus.Failed;
        FailureReason = failureReason.Trim();
        UpdatedAtUtc = failedAtUtc;
    }

    private void EnsurePending()
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException(
                $"A transaction with status '{Status}' cannot be changed.");
        }
    }
}