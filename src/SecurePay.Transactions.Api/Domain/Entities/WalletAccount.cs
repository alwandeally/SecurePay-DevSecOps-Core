namespace SecurePay.Transactions.Api.Domain.Entities;

public sealed class WalletAccount
{
    public const decimal MaximumBalance =
        9_999_999_999_999_999.99m;

    private WalletAccount()
    {
    }

    private WalletAccount(
        Guid userId,
        string currency)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Currency = currency;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public uint Version { get; private set; }

    public Guid UserId { get; private set; }

    public string Currency { get; private set; } =
        string.Empty;

    public decimal Balance { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public static WalletAccount Create(
        Guid userId,
        string currency)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID is required.",
                nameof(userId));
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

        return new WalletAccount(
            userId,
            normalizedCurrency);
    }

    public void Credit(
        decimal amount,
        DateTimeOffset occurredAtUtc)
    {
        EnsureValidAmount(amount);

        if (amount > MaximumBalance - Balance)
        {
            throw new InvalidOperationException(
                "The wallet balance limit would be exceeded.");
        }

        Balance += amount;
        UpdatedAtUtc = occurredAtUtc;
    }

    public void Debit(
        decimal amount,
        DateTimeOffset occurredAtUtc)
    {
        EnsureValidAmount(amount);

        if (amount > Balance)
        {
            throw new InvalidOperationException(
                "The wallet has insufficient funds.");
        }

        Balance -= amount;
        UpdatedAtUtc = occurredAtUtc;
    }

    private static void EnsureValidAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "The amount must be greater than zero.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentException(
                "The amount cannot have more than two decimal places.",
                nameof(amount));
        }
    }
}