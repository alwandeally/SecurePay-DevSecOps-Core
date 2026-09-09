using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecurePay.Transactions.Api.Domain.Entities;

namespace SecurePay.Transactions.Api.Persistence.Configurations;

public sealed class LedgerEntryConfiguration
    : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(
        EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable(
            "ledger_entries",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_ledger_entries_amount_positive",
                    "amount > 0");

                table.HasCheckConstraint(
                    "ck_ledger_entries_direction",
                    "direction IN ('Debit', 'Credit')");
            });

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(entry => entry.LedgerPostingId)
            .HasColumnName("ledger_posting_id")
            .IsRequired();

        builder.Property(entry => entry.LedgerAccountId)
            .HasColumnName("ledger_account_id")
            .IsRequired();

        builder.Property(entry => entry.Direction)
            .HasColumnName("direction")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(entry => entry.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(entry => entry.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasOne<LedgerAccount>()
            .WithMany()
            .HasForeignKey(
                entry => entry.LedgerAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(
                entry => new
                {
                    entry.LedgerPostingId,
                    entry.LedgerAccountId
                })
            .IsUnique()
            .HasDatabaseName(
                "ux_ledger_entries_posting_account");

        builder.HasIndex(
                entry => entry.LedgerAccountId)
            .HasDatabaseName(
                "ix_ledger_entries_account_id");
    }
}