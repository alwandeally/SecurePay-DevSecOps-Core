using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecurePay.Transactions.Api.Domain.Entities;

namespace SecurePay.Transactions.Api.Persistence.Configurations;

public sealed class WalletAccountConfiguration
    : IEntityTypeConfiguration<WalletAccount>
{
    public void Configure(
        EntityTypeBuilder<WalletAccount> builder)
    {
        builder.ToTable(
            "wallet_accounts",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_wallet_accounts_balance_non_negative",
                    "balance >= 0");
            });

        builder.HasKey(wallet => wallet.Id);

        builder.Property(wallet => wallet.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(wallet => wallet.Version)
            .IsRowVersion();

        builder.Property(wallet => wallet.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(wallet => wallet.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(wallet => wallet.Balance)
            .HasColumnName("balance")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(wallet => wallet.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(wallet => wallet.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(
                wallet => new
                {
                    wallet.UserId,
                    wallet.Currency
                })
            .IsUnique()
            .HasDatabaseName(
                "ux_wallet_accounts_user_id_currency");
    }
}