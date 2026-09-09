using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SecurePay.Transactions.Api.Domain.Entities;

namespace SecurePay.Transactions.Api.Persistence;

public sealed class TransactionsDbContext(
    DbContextOptions<TransactionsDbContext> options)
    : DbContext(options)
{
    public DbSet<PaymentTransaction> PaymentTransactions =>
        Set<PaymentTransaction>();

    public DbSet<WalletAccount> WalletAccounts =>
        Set<WalletAccount>();

    public DbSet<LedgerAccount> LedgerAccounts =>
        Set<LedgerAccount>();

    public DbSet<LedgerPosting> LedgerPostings =>
        Set<LedgerPosting>();

    public DbSet<LedgerEntry> LedgerEntries =>
        Set<LedgerEntry>();

    public override int SaveChanges(
        bool acceptAllChangesOnSuccess)
    {
        EnsureLedgerRecordsAreAppendOnly();

        return base.SaveChanges(
            acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnsureLedgerRecordsAreAppendOnly();

        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess,
            cancellationToken);
    }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TransactionsDbContext).Assembly);
    }

    private void EnsureLedgerRecordsAreAppendOnly()
    {
        var forbiddenEntry =
            ChangeTracker.Entries()
                .FirstOrDefault(entry =>
                    IsLedgerRecord(entry) &&
                    entry.State is
                        EntityState.Modified or
                        EntityState.Deleted);

        if (forbiddenEntry is null)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{forbiddenEntry.Metadata.ClrType.Name} records are append-only and cannot be modified or deleted.");
    }

    private static bool IsLedgerRecord(
        EntityEntry entry)
    {
        return entry.Entity is
            LedgerAccount or
            LedgerPosting or
            LedgerEntry;
    }
}