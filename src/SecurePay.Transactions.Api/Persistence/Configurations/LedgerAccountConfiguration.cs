using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecurePay.Transactions.Api.Domain.Entities;
using SecurePay.Transactions.Api.Domain.Enums;

namespace SecurePay.Transactions.Api.Persistence.Configurations;

public sealed class LedgerAccountConfiguration
    : IEntityTypeConfiguration<LedgerAccount>
{
    public void Configure(
        EntityTypeBuilder<LedgerAccount> builder)
    {
        builder.ToTable(
            "ledger_accounts",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_ledger_accounts_type_link",
                    """
                    (account_type = 'CustomerWallet' AND wallet_id IS NOT NULL)
                    OR
                    (account_type = 'PlatformClearing' AND wallet_id IS NULL)
                    """);
            });

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(account => account.AccountType)
            .HasColumnName("account_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(account => account.WalletId)
            .HasColumnName("wallet_id");

        builder.Property(account => account.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(account => account.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasOne<WalletAccount>()
            .WithOne()
            .HasForeignKey<LedgerAccount>(
                account => account.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(account => account.WalletId)
            .IsUnique()
            .HasFilter("wallet_id IS NOT NULL")
            .HasDatabaseName(
                "ux_ledger_accounts_wallet_id");

        builder.HasIndex(
                account => new
                {
                    account.AccountType,
                    account.Currency
                })
            .IsUnique()
            .HasFilter("wallet_id IS NULL")
            .HasDatabaseName(
                "ux_ledger_accounts_platform_type_currency");
    }
}