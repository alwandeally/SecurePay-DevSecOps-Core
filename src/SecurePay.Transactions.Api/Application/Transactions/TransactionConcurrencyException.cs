namespace SecurePay.Transactions.Api.Application.Transactions;

public sealed class TransactionConcurrencyException(
    Guid transactionId,
    Exception innerException)
    : Exception(
        $"Transaction '{transactionId}' was updated by another operation. Refresh and retry.",
        innerException);
