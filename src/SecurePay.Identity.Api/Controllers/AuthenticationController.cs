using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurePay.Identity.Api.Application.Authentication;
using SecurePay.Identity.Api.Contracts.Authentication;

namespace SecurePay.Identity.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController(
    IUserRegistrationService registrationService,
    IUserLoginService loginService)
    : ControllerBase
{
    [AllowAnonymous]
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
            var response =
                await registrationService.RegisterAsync(
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
                Detail =
                    "An account with this email address already exists."
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await loginService.LoginAsync(
                request,
                cancellationToken);

            return Ok(response);
        }
        catch (InvalidCredentialsException)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication failed",
                Detail =
                    "The supplied email address or password is invalid."
            });
        }
        catch (AccountLockedException exception)
        {
            var remainingLockout =
                exception.RetryAfterUtc -
                DateTimeOffset.UtcNow;

            var retryAfterSeconds = Math.Max(
                1,
                (int)Math.Ceiling(
                    remainingLockout.TotalSeconds));

            Response.Headers["Retry-After"] =
                retryAfterSeconds.ToString(
                    CultureInfo.InvariantCulture);

            return StatusCode(
                StatusCodes.Status429TooManyRequests,
                new ProblemDetails
                {
                    Status =
                        StatusCodes.Status429TooManyRequests,
                    Title =
                        "Authentication temporarily unavailable",
                    Detail =
                        "Too many unsuccessful login attempts. " +
                        "Please try again later."
                });
        }
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> GetCurrentUser()
    {
        var userIdValue = User
            .FindFirst(JwtRegisteredClaimNames.Sub)?
            .Value;

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var response = new CurrentUserResponse(
            userId,
            User.FindFirst(
                JwtRegisteredClaimNames.Email)?
                .Value ?? string.Empty,
            User.FindFirst(
                JwtRegisteredClaimNames.GivenName)?
                .Value ?? string.Empty,
            User.FindFirst(
                JwtRegisteredClaimNames.FamilyName)?
                .Value ?? string.Empty,
            User.FindFirst("role")?
                .Value ?? string.Empty);

        return Ok(response);
    }
}