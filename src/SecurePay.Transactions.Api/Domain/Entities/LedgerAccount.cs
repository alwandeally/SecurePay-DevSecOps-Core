using SecurePay.Transactions.Api.Domain.Enums;

namespace SecurePay.Transactions.Api.Domain.Entities;

public sealed class LedgerAccount
{
    private LedgerAccount()
    {
    }

    private LedgerAccount(
        LedgerAccountType accountType,
        Guid? walletId,
        string currency)
    {
        Id = Guid.NewGuid();
        AccountType = accountType;
        WalletId = walletId;
        Currency = currency;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public LedgerAccountType AccountType { get; private set; }

    public Guid? WalletId { get; private set; }

    public string Currency { get; private set; } =
        string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static LedgerAccount CreateCustomerWallet(
        Guid walletId,
        string currency)
    {
        if (walletId == Guid.Empty)
        {
            throw new ArgumentException(
                "Wallet ID is required.",
                nameof(walletId));
        }

        return new LedgerAccount(
            LedgerAccountType.CustomerWallet,
            walletId,
            NormalizeCurrency(currency));
    }

    public static LedgerAccount CreatePlatformClearing(
        string currency)
    {
        return new LedgerAccount(
            LedgerAccountType.PlatformClearing,
            null,
            NormalizeCurrency(currency));
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