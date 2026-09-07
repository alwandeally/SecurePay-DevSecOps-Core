using System.ComponentModel.DataAnnotations;

namespace SecurePay.Transactions.Api.Contracts.Transactions;

public sealed record FailTransactionRequest(
    [property: Required]
    [property: StringLength(500, MinimumLength = 1)]
    string FailureReason);
