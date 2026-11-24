using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SecurePay.Identity.Api.Application.Bootstrap;
using SecurePay.Identity.Api.Configuration;
using SecurePay.Identity.Api.Domain.Entities;
using SecurePay.Identity.Api.Domain.Enums;
using SecurePay.Identity.Api.Persistence;
using Xunit;

namespace SecurePay.Identity.UnitTests.Application.Bootstrap;

public sealed class OperationsAccountBootstrapperTests
{
    [Fact]
    public async Task BootstrapAsync_WhenDisabled_DoesNotCreateAccount()
    {
        await using var dbContext = CreateDbContext();

        var bootstrapper = CreateBootstrapper(
            dbContext,
            new OperationsBootstrapOptions
            {
                Enabled = false
            });

        await bootstrapper.BootstrapAsync();

        Assert.Empty(await dbContext.Users.ToListAsync());
    }

    [Fact]
    public async Task BootstrapAsync_WhenEnabled_CreatesOperationsAccount()
    {
        await using var dbContext = CreateDbContext();

        var configuration = CreateEnabledConfiguration();
        var passwordHasher = new PasswordHasher<UserAccount>();

        var bootstrapper = CreateBootstrapper(
            dbContext,
            configuration,
            passwordHasher);

        await bootstrapper.BootstrapAsync();

        var account = await dbContext.Users.SingleAsync();

        Assert.Equal(
            "operations@securepay.local",
            account.Email);
        Assert.Equal(
            "OPERATIONS@SECUREPAY.LOCAL",
            account.NormalizedEmail);
        Assert.Equal(UserRole.Operations, account.Role);
        Assert.True(account.IsActive);

        var verificationResult =
            passwordHasher.VerifyHashedPassword(
                account,
                account.PasswordHash,
                configuration.Password);

        Assert.NotEqual(
            PasswordVerificationResult.Failed,
            verificationResult);
    }

    [Fact]
    public async Task BootstrapAsync_WhenRunTwice_IsIdempotent()
    {
        await using var dbContext = CreateDbContext();

        var bootstrapper = CreateBootstrapper(
            dbContext,
            CreateEnabledConfiguration());

        await bootstrapper.BootstrapAsync();
        await bootstrapper.BootstrapAsync();

        Assert.Equal(
            1,
            await dbContext.Users.CountAsync());
    }

    [Fact]
    public async Task BootstrapAsync_WhenEmailBelongsToCustomer_RefusesRoleEscalation()
    {
        await using var dbContext = CreateDbContext();

        var customer = UserAccount.Register(
            "operations@securepay.local",
            "Existing",
            "Customer",
            _ => "existing-customer-password-hash");

        dbContext.Users.Add(customer);
        await dbContext.SaveChangesAsync();

        var bootstrapper = CreateBootstrapper(
            dbContext,
            CreateEnabledConfiguration());

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => bootstrapper.BootstrapAsync());

        Assert.Contains(
            "Automatic role escalation was refused",
            exception.Message);

        var persistedAccount =
            await dbContext.Users.SingleAsync();

        Assert.Equal(UserRole.Customer, persistedAccount.Role);
        Assert.Equal(1, await dbContext.Users.CountAsync());
    }

    private static IdentityDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseInMemoryDatabase(
                    $"identity-tests-{Guid.NewGuid()}")
                .Options;

        return new IdentityDbContext(options);
    }

    private static OperationsAccountBootstrapper CreateBootstrapper(
        IdentityDbContext dbContext,
        OperationsBootstrapOptions configuration,
        IPasswordHasher<UserAccount>? passwordHasher = null)
    {
        return new OperationsAccountBootstrapper(
            dbContext,
            passwordHasher ??
                new PasswordHasher<UserAccount>(),
            Options.Create(configuration),
            NullLogger<OperationsAccountBootstrapper>.Instance);
    }

    private static OperationsBootstrapOptions
        CreateEnabledConfiguration()
    {
        return new OperationsBootstrapOptions
        {
            Enabled = true,
            Email = "operations@securepay.local",
            Password =
                "Unit-test-operations-password-123!",
            FirstName = "SecurePay",
            LastName = "Operations"
        };
    }
}
