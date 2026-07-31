using System.ComponentModel.DataAnnotations;

namespace SecurePay.Transactions.Api.Contracts.Transactions;

public sealed class CreateTransactionRequest
{
    [Required]
    public string TransactionType { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Amount { get; init; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string IdempotencyKey { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }
}
