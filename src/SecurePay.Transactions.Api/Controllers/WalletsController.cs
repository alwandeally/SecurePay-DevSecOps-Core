using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurePay.Transactions.Api.Application.Wallets;
using SecurePay.Transactions.Api.Contracts.Wallets;

namespace SecurePay.Transactions.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/wallets")]
public sealed class WalletsController(
    IWalletQueryService walletQueryService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<WalletResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<
        ActionResult<IReadOnlyList<WalletResponse>>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(
                out var userId))
        {
            return Unauthorized();
        }

        var response =
            await walletQueryService.GetAllAsync(
                userId,
                cancellationToken);

        return Ok(response);
    }

    [HttpGet("{currency}")]
    [ProducesResponseType<WalletResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WalletResponse>>
        GetByCurrencyAsync(
            string currency,
            CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(
                out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var response =
                await walletQueryService
                    .GetByCurrencyAsync(
                        userId,
                        currency,
                        cancellationToken);

            if (response is null)
            {
                return NotFound();
            }

            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return CreateBadRequest(exception);
        }
    }

    [HttpGet("{currency}/ledger-entries")]
    [ProducesResponseType<PagedLedgerEntriesResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<PagedLedgerEntriesResponse>>
        GetLedgerEntriesAsync(
            string currency,
            CancellationToken cancellationToken,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
    {
        if (!TryGetAuthenticatedUserId(
                out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var response =
                await walletQueryService
                    .GetLedgerEntriesAsync(
                        userId,
                        currency,
                        page,
                        pageSize,
                        cancellationToken);

            if (response is null)
            {
                return NotFound();
            }

            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return CreateBadRequest(exception);
        }
    }

    private bool TryGetAuthenticatedUserId(
        out Guid userId)
    {
        var subject =
            User.FindFirstValue(
                JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return Guid.TryParse(
            subject,
            out userId);
    }

    private BadRequestObjectResult CreateBadRequest(
        ArgumentException exception)
    {
        return BadRequest(
            new ProblemDetails
            {
                Title = "Invalid wallet request",
                Detail = exception.Message,
                Status =
                    StatusCodes.Status400BadRequest
            });
    }
}