using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurePay.Transactions.Api.Application.Operations;
using SecurePay.Transactions.Api.Authorization;
using SecurePay.Transactions.Api.Contracts.Operations;

namespace SecurePay.Transactions.Api.Controllers;

[ApiController]
[Authorize(
    Policy = AuthorizationPolicies.TransactionProcessor)]
[Route("api/operations/transactions")]
public sealed class OperationsTransactionsController(
    IOperationsTransactionQueryService queryService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedOperationsTransactionResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<
        ActionResult<PagedOperationsTransactionResponse>> GetQueue(
        [FromQuery] OperationsTransactionQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var response =
                await queryService.QueryAsync(
                    query,
                    cancellationToken);

            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid operations transaction query",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}
