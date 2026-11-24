using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecurePay.Transactions.Api.Domain.Entities;

namespace SecurePay.Transactions.Api.Persistence.Configurations;

public sealed class TransactionLifecycleAuditEntryConfiguration
    : IEntityTypeConfiguration<TransactionLifecycleAuditEntry>
{
    public void Configure(EntityTypeBuilder<TransactionLifecycleAuditEntry> builder)
    {
        builder.ToTable(
            "transaction_lifecycle_audit_entries",
            table => table.HasCheckConstraint(
                "ck_transaction_lifecycle_audit_action_status",
                """
                (action = 'Complete' AND resulting_status = 'Completed')
                OR (action = 'Fail' AND resulting_status = 'Failed')
                """));

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(entry => entry.TransactionId)
            .HasColumnName("transaction_id")
            .IsRequired();

        builder.Property(entry => entry.OperatorId)
            .HasColumnName("operator_id")
            .IsRequired();

        builder.Property(entry => entry.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .IsRequired();

        builder.Property(entry => entry.Action)
            .HasColumnName("action")
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(entry => entry.ResultingStatus)
            .HasColumnName("resulting_status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.HasOne<PaymentTransaction>()
            .WithMany()
            .HasForeignKey(entry => entry.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entry => entry.TransactionId)
            .HasDatabaseName("ix_transaction_lifecycle_audit_transaction_id");
    }
}
