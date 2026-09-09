using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurePay.Transactions.Api.Application.Transactions;
using SecurePay.Transactions.Api.Contracts.Transactions;
using SecurePay.Transactions.Api.Controllers;

namespace SecurePay.Transactions.UnitTests.Controllers;

public sealed class TransactionConcurrencyControllerTests
{
    [Fact]
    public async Task Complete_WhenConcurrentUpdateOccurs_ReturnsConflict()
    {
        var transactionId = Guid.NewGuid();

        var controller =
            new TransactionLifecycleController(
                new ConcurrencyThrowingTransactionService());

        var result =
            await controller.Complete(
                transactionId,
                CancellationToken.None);

        AssertConcurrencyConflict(
            result,
            transactionId);
    }

    [Fact]
    public async Task Fail_WhenConcurrentUpdateOccurs_ReturnsConflict()
    {
        var transactionId = Guid.NewGuid();

        var controller =
            new TransactionLifecycleController(
                new ConcurrencyThrowingTransactionService());

        var request =
            new FailTransactionRequest(
                "Payment processor rejected the transaction.");

        var result =
            await controller.Fail(
                transactionId,
                request,
                CancellationToken.None);

        AssertConcurrencyConflict(
            result,
            transactionId);
    }

    private static void AssertConcurrencyConflict(
        ActionResult<TransactionResponse> result,
        Guid transactionId)
    {
        var conflict =
            Assert.IsType<ConflictObjectResult>(
                result.Result);

        var problemDetails =
            Assert.IsType<ProblemDetails>(
                conflict.Value);

        Assert.Equal(
            StatusCodes.Status409Conflict,
            conflict.StatusCode);

        Assert.Equal(
            StatusCodes.Status409Conflict,
            problemDetails.Status);

        Assert.Equal(
            "Concurrent transaction update",
            problemDetails.Title);

        Assert.Equal(
            $"Transaction '{transactionId}' was updated by another operation. Refresh and retry.",
            problemDetails.Detail);
    }

    private sealed class ConcurrencyThrowingTransactionService
        : ITransactionService
    {
        public Task<TransactionResponse> CreateAsync(
            Guid userId,
            CreateTransactionRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<TransactionResponse?> GetByIdAsync(
            Guid userId,
            Guid transactionId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<TransactionResponse>> GetAllAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<TransactionResponse?> CompleteAsync(
            Guid transactionId,
            CancellationToken cancellationToken)
        {
            return Task.FromException<TransactionResponse?>(
                CreateException(transactionId));
        }

        public Task<TransactionResponse?> FailAsync(
            Guid transactionId,
            string failureReason,
            CancellationToken cancellationToken)
        {
            return Task.FromException<TransactionResponse?>(
                CreateException(transactionId));
        }

        private static TransactionConcurrencyException CreateException(
            Guid transactionId)
        {
            return new TransactionConcurrencyException(
                transactionId,
                new DbUpdateConcurrencyException(
                    "The transaction version is stale."));
        }
    }
}