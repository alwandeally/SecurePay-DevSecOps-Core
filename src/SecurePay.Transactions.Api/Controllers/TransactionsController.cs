using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurePay.Transactions.Api.Application.Transactions;
using SecurePay.Transactions.Api.Contracts.Transactions;

namespace SecurePay.Transactions.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/transactions")]
public sealed class TransactionsController(
    ITransactionService transactionService)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<TransactionResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TransactionResponse>> Create(
        [FromBody] CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var response =
                await transactionService.CreateAsync(
                    userId,
                    request,
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    transactionId = response.TransactionId
                },
                response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid transaction request",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpGet("{transactionId:guid}")]
    [ProducesResponseType<TransactionResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> GetById(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized();
        }

        var response =
            await transactionService.GetByIdAsync(
                userId,
                transactionId,
                cancellationToken);

        return response is null
            ? NotFound()
            : Ok(response);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<
        IReadOnlyList<TransactionResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized();
        }

        var response =
            await transactionService.GetAllAsync(
                userId,
                cancellationToken);

        return Ok(response);
    }

    private bool TryGetAuthenticatedUserId(
        out Guid userId)
    {
        var userIdValue =
            User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdValue, out userId) &&
               userId != Guid.Empty;
    }
}