using System.ComponentModel.DataAnnotations;

namespace SecurePay.Transactions.Api.Contracts.Transactions;

public sealed record FailTransactionRequest(
    [Required]
    [MaxLength(500)]
    string FailureReason);
