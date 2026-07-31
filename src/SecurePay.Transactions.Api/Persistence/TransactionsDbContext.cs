using Microsoft.EntityFrameworkCore;
using SecurePay.Transactions.Api.Domain.Entities;

namespace SecurePay.Transactions.Api.Persistence;

public sealed class TransactionsDbContext(
    DbContextOptions<TransactionsDbContext> options)
    : DbContext(options)
{
    public DbSet<PaymentTransaction> PaymentTransactions =>
        Set<PaymentTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TransactionsDbContext).Assembly);
    }
}
