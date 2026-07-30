using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SecurePay.Identity.Api.Contracts.Authentication;
using SecurePay.Identity.Api.Domain.Entities;
using SecurePay.Identity.Api.Persistence;

namespace SecurePay.Identity.Api.Application.Authentication;

public sealed class UserLoginService(
    IdentityDbContext dbContext,
    IPasswordHasher<UserAccount> passwordHasher,
    IJwtTokenService jwtTokenService)
    : IUserLoginService
{
    private const int MaximumFailedAttempts = 5;

    private static readonly TimeSpan LockoutDuration =
        TimeSpan.FromMinutes(15);

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedEmail = request.Email
            .Trim()
            .ToUpperInvariant();

        var user = await dbContext.Users
            .SingleOrDefaultAsync(
                account =>
                    account.NormalizedEmail == normalizedEmail,
                cancellationToken);

        // Use the same error for missing, inactive or incorrectly
        // authenticated accounts to reduce account enumeration.
        if (user is null || !user.IsActive)
            throw new InvalidCredentialsException();

        var occurredAtUtc = DateTimeOffset.UtcNow;

        if (user.IsLockedOut(occurredAtUtc))
        {
            throw new AccountLockedException(
                user.LockoutEndUtc!.Value);
        }

        var verificationResult =
            passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

        if (verificationResult ==
            PasswordVerificationResult.Failed)
        {
            user.RecordFailedLogin(
                occurredAtUtc,
                MaximumFailedAttempts,
                LockoutDuration);

            await dbContext.SaveChangesAsync(
                cancellationToken);

            if (user.IsLockedOut(occurredAtUtc))
            {
                throw new AccountLockedException(
                    user.LockoutEndUtc!.Value);
            }

            throw new InvalidCredentialsException();
        }

        if (verificationResult ==
            PasswordVerificationResult.SuccessRehashNeeded)
        {
            var improvedPasswordHash =
                passwordHasher.HashPassword(
                    user,
                    request.Password);

            user.ChangePasswordHash(
                improvedPasswordHash,
                occurredAtUtc);
        }

        user.RecordSuccessfulLogin(occurredAtUtc);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return jwtTokenService.CreateAccessToken(user);
    }
}