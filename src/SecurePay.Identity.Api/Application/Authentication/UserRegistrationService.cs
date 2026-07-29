using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SecurePay.Identity.Api.Contracts.Authentication;
using SecurePay.Identity.Api.Domain.Entities;
using SecurePay.Identity.Api.Persistence;

namespace SecurePay.Identity.Api.Application.Authentication;

public sealed class UserRegistrationService(
    IdentityDbContext dbContext,
    IPasswordHasher<UserAccount> passwordHasher)
    : IUserRegistrationService
{
    public async Task<RegisterUserResponse> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email
            .Trim()
            .ToUpperInvariant();

        var emailAlreadyExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken);

        if (emailAlreadyExists)
            throw new DuplicateEmailException(request.Email);

        var user = UserAccount.Register(
            request.Email,
            request.FirstName,
            request.LastName,
            account => passwordHasher.HashPassword(
                account,
                request.Password));

        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsDuplicateEmailViolation(exception))
        {
            throw new DuplicateEmailException(
                request.Email,
                exception);
        }

        return new RegisterUserResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role.ToString(),
            user.CreatedAtUtc);
    }

    private static bool IsDuplicateEmailViolation(
        DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_user_accounts_normalized_email"
        };
    }
}