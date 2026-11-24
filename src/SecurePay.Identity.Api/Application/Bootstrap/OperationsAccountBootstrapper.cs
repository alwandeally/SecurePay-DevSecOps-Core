using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using SecurePay.Identity.Api.Configuration;
using SecurePay.Identity.Api.Domain.Entities;
using SecurePay.Identity.Api.Domain.Enums;
using SecurePay.Identity.Api.Persistence;

namespace SecurePay.Identity.Api.Application.Bootstrap;

public sealed class OperationsAccountBootstrapper(
    IdentityDbContext dbContext,
    IPasswordHasher<UserAccount> passwordHasher,
    IOptions<OperationsBootstrapOptions> options,
    ILogger<OperationsAccountBootstrapper> logger)
    : IOperationsAccountBootstrapper
{
    public async Task BootstrapAsync(
        CancellationToken cancellationToken = default)
    {
        var configuration = options.Value;

        if (!configuration.Enabled)
        {
            logger.LogInformation(
                "Operations account bootstrap is disabled.");

            return;
        }

        var normalizedEmail = configuration.Email
            .Trim()
            .ToUpperInvariant();

        var existingAccount = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken);

        if (existingAccount is not null)
        {
            EnsureOperationsAccount(existingAccount);

            logger.LogInformation(
                "Operations account {UserId} already exists.",
                existingAccount.Id);

            return;
        }

        var operationsAccount = UserAccount.CreateOperations(
            configuration.Email,
            configuration.FirstName,
            configuration.LastName,
            account => passwordHasher.HashPassword(
                account,
                configuration.Password));

        dbContext.Users.Add(operationsAccount);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsDuplicateEmailViolation(exception))
        {
            dbContext.Entry(operationsAccount).State =
                EntityState.Detached;

            var concurrentlyCreatedAccount =
                await dbContext.Users
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        user =>
                            user.NormalizedEmail ==
                            normalizedEmail,
                        cancellationToken);

            if (concurrentlyCreatedAccount is null)
                throw;

            EnsureOperationsAccount(
                concurrentlyCreatedAccount);

            logger.LogInformation(
                "Operations account {UserId} was created " +
                "by another application instance.",
                concurrentlyCreatedAccount.Id);

            return;
        }

        logger.LogInformation(
            "Operations account {UserId} was created successfully.",
            operationsAccount.Id);
    }

    private static void EnsureOperationsAccount(
        UserAccount existingAccount)
    {
        if (existingAccount.Role == UserRole.Operations)
            return;

        throw new InvalidOperationException(
            "The configured Operations bootstrap email is " +
            "already assigned to a non-Operations account. " +
            "Automatic role escalation was refused.");
    }

    private static bool IsDuplicateEmailViolation(
        DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName:
                "ux_user_accounts_normalized_email"
        };
    }
}
