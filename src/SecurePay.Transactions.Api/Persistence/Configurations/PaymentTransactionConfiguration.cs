using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecurePay.Transactions.Api.Domain.Entities;

namespace SecurePay.Transactions.Api.Persistence.Configurations;

public sealed class PaymentTransactionConfiguration
    : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(
        EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("payment_transactions");

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(transaction => transaction.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(transaction => transaction.Type)
            .HasColumnName("transaction_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(transaction => transaction.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(transaction => transaction.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(transaction => transaction.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(transaction => transaction.Reference)
            .HasColumnName("reference")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(transaction => transaction.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(transaction => transaction.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(transaction => transaction.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(500);

        builder.Property(transaction => transaction.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(transaction => transaction.CompletedAtUtc)
            .HasColumnName("completed_at_utc");

        builder.Property(transaction => transaction.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(transaction => transaction.Reference)
            .IsUnique()
            .HasDatabaseName("ux_payment_transactions_reference");

        builder.HasIndex(transaction => new
            {
                transaction.UserId,
                transaction.IdempotencyKey
            })
            .IsUnique()
            .HasDatabaseName(
                "ux_payment_transactions_user_id_idempotency_key");

        builder.HasIndex(transaction => new
            {
                transaction.UserId,
                transaction.CreatedAtUtc
            })
            .HasDatabaseName(
                "ix_payment_transactions_user_id_created_at_utc");
    }
}
