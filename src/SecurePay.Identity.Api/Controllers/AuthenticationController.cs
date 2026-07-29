using Microsoft.AspNetCore.Mvc;
using SecurePay.Identity.Api.Application.Authentication;
using SecurePay.Identity.Api.Contracts.Authentication;

namespace SecurePay.Identity.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController(
    IUserRegistrationService registrationService)
    : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<RegisterUserResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterUserResponse>> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await registrationService.RegisterAsync(
                request,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                response);
        }
        catch (DuplicateEmailException)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Registration failed",
                Detail = "An account with this email address already exists."
            });
        }
    }
}
