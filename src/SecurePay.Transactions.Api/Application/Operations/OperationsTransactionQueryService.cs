using Microsoft.EntityFrameworkCore;
using SecurePay.Transactions.Api.Contracts.Operations;
using SecurePay.Transactions.Api.Contracts.Transactions;
using SecurePay.Transactions.Api.Domain.Entities;
using SecurePay.Transactions.Api.Domain.Enums;
using SecurePay.Transactions.Api.Persistence;

using PaymentStatus =
    SecurePay.Transactions.Api.Domain.Enums.TransactionStatus;

namespace SecurePay.Transactions.Api.Application.Operations;

public sealed class OperationsTransactionQueryService(
    TransactionsDbContext dbContext)
    : IOperationsTransactionQueryService
{
    public async Task<PagedOperationsTransactionResponse> QueryAsync(
        OperationsTransactionQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        ValidatePagination(query);
        ValidateDateRange(query);

        if (query.UserId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(query.UserId));
        }

        var status = ParseOptionalEnum<PaymentStatus>(
            query.Status,
            nameof(query.Status),
            "Pending, Completed, or Failed");

        var transactionType = ParseOptionalEnum<TransactionType>(
            query.TransactionType,
            nameof(query.TransactionType),
            "Deposit, Withdrawal, or Transfer");

        IQueryable<PaymentTransaction> transactions =
            dbContext.PaymentTransactions
                .AsNoTracking();

        if (status.HasValue)
        {
            transactions = transactions.Where(
                transaction =>
                    transaction.Status == status.Value);
        }

        if (transactionType.HasValue)
        {
            transactions = transactions.Where(
                transaction =>
                    transaction.Type == transactionType.Value);
        }

        if (query.UserId.HasValue)
        {
            transactions = transactions.Where(
                transaction =>
                    transaction.UserId == query.UserId.Value);
        }

        if (query.CreatedFromUtc.HasValue)
        {
            transactions = transactions.Where(
                transaction =>
                    transaction.CreatedAtUtc >=
                    query.CreatedFromUtc.Value);
        }

        if (query.CreatedToUtc.HasValue)
        {
            transactions = transactions.Where(
                transaction =>
                    transaction.CreatedAtUtc <=
                    query.CreatedToUtc.Value);
        }

        var totalCount =
            await transactions.CountAsync(
                cancellationToken);

        var requestedSkip =
            ((long)query.Page - 1) *
            query.PageSize;

        if (requestedSkip > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query.Page),
                "The requested page is too large.");
        }

        var pageTransactions =
            await transactions
                .OrderByDescending(transaction =>
                    transaction.CreatedAtUtc)
                .ThenByDescending(transaction =>
                    transaction.Id)
                .Skip((int)requestedSkip)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

        var items =
            pageTransactions
                .Select(MapResponse)
                .ToArray();

        var totalPages =
            totalCount == 0
                ? 0
                : (totalCount + query.PageSize - 1) /
                  query.PageSize;

        return new PagedOperationsTransactionResponse(
            Items: items,
            Page: query.Page,
            PageSize: query.PageSize,
            TotalCount: totalCount,
            TotalPages: totalPages);
    }

    private static void ValidatePagination(
        OperationsTransactionQuery query)
    {
        if (query.Page < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query.Page),
                "Page must be at least 1.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query.PageSize),
                "Page size must be between 1 and 100.");
        }
    }

    private static void ValidateDateRange(
        OperationsTransactionQuery query)
    {
        if (query.CreatedFromUtc.HasValue &&
            query.CreatedToUtc.HasValue &&
            query.CreatedFromUtc.Value >
            query.CreatedToUtc.Value)
        {
            throw new ArgumentException(
                "CreatedFromUtc cannot be later than CreatedToUtc.",
                nameof(query.CreatedFromUtc));
        }
    }

    private static TEnum? ParseOptionalEnum<TEnum>(
        string? value,
        string parameterName,
        string allowedValues)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Enum.TryParse<TEnum>(
                value.Trim(),
                ignoreCase: true,
                out var parsedValue) ||
            !Enum.IsDefined(
                typeof(TEnum),
                parsedValue))
        {
            throw new ArgumentException(
                $"Value must be one of: {allowedValues}.",
                parameterName);
        }

        return parsedValue;
    }

    private static TransactionResponse MapResponse(
        PaymentTransaction transaction)
    {
        return new TransactionResponse(
            TransactionId: transaction.Id,
            UserId: transaction.UserId,
            DestinationUserId:
                transaction.DestinationUserId,
            Reference: transaction.Reference,
            TransactionType:
                transaction.Type.ToString(),
            Amount: transaction.Amount,
            Currency: transaction.Currency,
            Status: transaction.Status.ToString(),
            Description: transaction.Description,
            FailureReason: transaction.FailureReason,
            CreatedAtUtc: transaction.CreatedAtUtc,
            CompletedAtUtc: transaction.CompletedAtUtc,
            UpdatedAtUtc: transaction.UpdatedAtUtc);
    }
}
