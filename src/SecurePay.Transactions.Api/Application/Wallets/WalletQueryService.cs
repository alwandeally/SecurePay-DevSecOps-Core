using Microsoft.EntityFrameworkCore;
using SecurePay.Transactions.Api.Contracts.Wallets;
using SecurePay.Transactions.Api.Persistence;

namespace SecurePay.Transactions.Api.Application.Wallets;

public sealed class WalletQueryService(
    TransactionsDbContext dbContext)
    : IWalletQueryService
{
    public async Task<IReadOnlyList<WalletResponse>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        EnsureValidUserId(userId);

        return await dbContext.WalletAccounts
            .AsNoTracking()
            .Where(wallet =>
                wallet.UserId == userId)
            .OrderBy(wallet =>
                wallet.Currency)
            .Select(wallet =>
                new WalletResponse(
                    wallet.Id,
                    wallet.Currency,
                    wallet.Balance,
                    wallet.CreatedAtUtc,
                    wallet.UpdatedAtUtc))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<WalletResponse?> GetByCurrencyAsync(
        Guid userId,
        string currency,
        CancellationToken cancellationToken)
    {
        EnsureValidUserId(userId);

        var normalizedCurrency =
            NormalizeCurrency(currency);

        return await dbContext.WalletAccounts
            .AsNoTracking()
            .Where(wallet =>
                wallet.UserId == userId &&
                wallet.Currency == normalizedCurrency)
            .Select(wallet =>
                new WalletResponse(
                    wallet.Id,
                    wallet.Currency,
                    wallet.Balance,
                    wallet.CreatedAtUtc,
                    wallet.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedLedgerEntriesResponse?>
        GetLedgerEntriesAsync(
            Guid userId,
            string currency,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
    {
        EnsureValidUserId(userId);

        var normalizedCurrency =
            NormalizeCurrency(currency);

        var skip =
            ValidatePagination(
                page,
                pageSize);

        var wallet =
            await dbContext.WalletAccounts
                .AsNoTracking()
                .Where(existing =>
                    existing.UserId == userId &&
                    existing.Currency ==
                        normalizedCurrency)
                .Select(existing => new
                {
                    existing.Id,
                    existing.Currency
                })
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (wallet is null)
        {
            return null;
        }

        var ledgerAccountId =
            await dbContext.LedgerAccounts
                .AsNoTracking()
                .Where(account =>
                    account.WalletId == wallet.Id)
                .Select(account =>
                    (Guid?)account.Id)
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (ledgerAccountId is null)
        {
            return new PagedLedgerEntriesResponse(
                wallet.Currency,
                page,
                pageSize,
                TotalCount: 0,
                TotalPages: 0,
                Items: []);
        }

        var ledgerEntriesQuery =
            from entry in
                dbContext.LedgerEntries.AsNoTracking()
            join posting in
                dbContext.LedgerPostings.AsNoTracking()
                on entry.LedgerPostingId equals posting.Id
            join transaction in
                dbContext.PaymentTransactions.AsNoTracking()
                on posting.PaymentTransactionId
                equals transaction.Id
            where entry.LedgerAccountId ==
                ledgerAccountId.Value
            select new
            {
                EntryId = entry.Id,
                PostingId = posting.Id,
                TransactionId = transaction.Id,
                TransactionReference =
                    transaction.Reference,
                TransactionType = transaction.Type,
                entry.Direction,
                entry.Amount,
                posting.Currency,
                transaction.Description,
                entry.CreatedAtUtc
            };

        var totalCount =
            await ledgerEntriesQuery.CountAsync(
                cancellationToken);

        var rows =
            await ledgerEntriesQuery
                .OrderByDescending(entry =>
                    entry.CreatedAtUtc)
                .ThenByDescending(entry =>
                    entry.EntryId)
                .Skip(skip)
                .Take(pageSize)
                .ToArrayAsync(cancellationToken);

        var items =
            rows.Select(entry =>
                    new LedgerEntryResponse(
                        entry.EntryId,
                        entry.PostingId,
                        entry.TransactionId,
                        entry.TransactionReference,
                        entry.TransactionType.ToString(),
                        entry.Direction.ToString(),
                        entry.Amount,
                        entry.Currency,
                        entry.Description,
                        entry.CreatedAtUtc))
                .ToArray();

        var totalPages =
            totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount /
                    (double)pageSize);

        return new PagedLedgerEntriesResponse(
            wallet.Currency,
            page,
            pageSize,
            totalCount,
            totalPages,
            items);
    }

    private static void EnsureValidUserId(
        Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "An authenticated user ID is required.",
                nameof(userId));
        }
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

    private static int ValidatePagination(
        int page,
        int pageSize)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(page),
                "Page must be greater than zero.");
        }

        if (pageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "Page size must be between 1 and 100.");
        }

        var skip =
            ((long)page - 1) * pageSize;

        if (skip > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(page),
                "The requested page is too large.");
        }

        return (int)skip;
    }
}