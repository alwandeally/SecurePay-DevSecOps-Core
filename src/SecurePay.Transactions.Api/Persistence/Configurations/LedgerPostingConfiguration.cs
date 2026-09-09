using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecurePay.Transactions.Api.Domain.Entities;

namespace SecurePay.Transactions.Api.Persistence.Configurations;

public sealed class LedgerPostingConfiguration
    : IEntityTypeConfiguration<LedgerPosting>
{
    public void Configure(
        EntityTypeBuilder<LedgerPosting> builder)
    {
        builder.ToTable("ledger_postings");

        builder.HasKey(posting => posting.Id);

        builder.Property(posting => posting.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(
                posting => posting.PaymentTransactionId)
            .HasColumnName("payment_transaction_id")
            .IsRequired();

        builder.Property(posting => posting.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(posting => posting.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasOne<PaymentTransaction>()
            .WithOne()
            .HasForeignKey<LedgerPosting>(
                posting =>
                    posting.PaymentTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(posting => posting.Entries)
            .WithOne()
            .HasForeignKey(
                entry => entry.LedgerPostingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(posting => posting.Entries)
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);

        builder.HasIndex(
                posting =>
                    posting.PaymentTransactionId)
            .IsUnique()
            .HasDatabaseName(
                "ux_ledger_postings_payment_transaction_id");
    }
}