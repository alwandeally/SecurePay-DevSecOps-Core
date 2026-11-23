using SecurePay.Transactions.Api.Contracts.Wallets;

namespace SecurePay.Transactions.Api.Application.Wallets;

public interface IWalletQueryService
{
    Task<IReadOnlyList<WalletResponse>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<WalletResponse?> GetByCurrencyAsync(
        Guid userId,
        string currency,
        CancellationToken cancellationToken);

    Task<PagedLedgerEntriesResponse?> GetLedgerEntriesAsync(
        Guid userId,
        string currency,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}