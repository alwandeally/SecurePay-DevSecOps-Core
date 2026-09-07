using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurePay.Transactions.Api.Application.Transactions;
using SecurePay.Transactions.Api.Authorization;
using SecurePay.Transactions.Api.Contracts.Transactions;

namespace SecurePay.Transactions.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.TransactionProcessor)]
[Route("api/operations/transactions")]
public sealed class TransactionLifecycleController(
    ITransactionService transactionService)
    : ControllerBase
{
    [HttpPost("{transactionId:guid}/complete")]
    [ProducesResponseType<TransactionResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TransactionResponse>> Complete(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response =
                await transactionService.CompleteAsync(
                    transactionId,
                    cancellationToken);

            return response is null
                ? NotFound()
                : Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Invalid transaction state",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpPost("{transactionId:guid}/fail")]
    [ProducesResponseType<TransactionResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TransactionResponse>> Fail(
        Guid transactionId,
        [FromBody] FailTransactionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response =
                await transactionService.FailAsync(
                    transactionId,
                    request.FailureReason,
                    cancellationToken);

            return response is null
                ? NotFound()
                : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid failure request",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Invalid transaction state",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }
}
