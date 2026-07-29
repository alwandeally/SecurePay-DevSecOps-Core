using SecurePay.Identity.Api.Domain.Entities;
using SecurePay.Identity.Api.Domain.Enums;

namespace SecurePay.Identity.UnitTests.Domain;

public sealed class UserAccountTests
{
    [Fact]
    public void Register_WithValidDetails_CreatesActiveCustomer()
    {
        var beforeRegistration = DateTimeOffset.UtcNow;

        var user = CreateUser();

        var afterRegistration = DateTimeOffset.UtcNow;

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("alwande.demo@securepay.local", user.Email);
        Assert.Equal(
            "ALWANDE.DEMO@SECUREPAY.LOCAL",
            user.NormalizedEmail);
        Assert.Equal("secure-password-hash", user.PasswordHash);
        Assert.Equal("Alwande", user.FirstName);
        Assert.Equal("Demo", user.LastName);
        Assert.Equal(UserRole.Customer, user.Role);
        Assert.True(user.IsActive);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.InRange(
            user.CreatedAtUtc,
            beforeRegistration,
            afterRegistration);
    }

    [Fact]
    public void Register_NormalizesAndTrimsUserInformation()
    {
        var user = UserAccount.Register(
            "  Alwande.Demo@SecurePay.Local  ",
            "  Alwande  ",
            "  Demo  ",
            _ => "secure-password-hash");

        Assert.Equal(
            "Alwande.Demo@SecurePay.Local",
            user.Email);
        Assert.Equal(
            "ALWANDE.DEMO@SECUREPAY.LOCAL",
            user.NormalizedEmail);
        Assert.Equal("Alwande", user.FirstName);
        Assert.Equal("Demo", user.LastName);
    }

    [Fact]
    public void Register_WhenHasherReturnsEmpty_ThrowsException()
    {
        var action = () => UserAccount.Register(
            "alwande.demo@securepay.local",
            "Alwande",
            "Demo",
            _ => string.Empty);

        var exception = Assert.Throws<InvalidOperationException>(
            action);

        Assert.Contains(
            "invalid hash",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RecordFailedLogin_WhenLimitReached_LocksAccount()
    {
        var user = CreateUser();
        var firstAttempt = new DateTimeOffset(
            2026, 7, 30, 10, 0, 0, TimeSpan.Zero);

        user.RecordFailedLogin(
            firstAttempt,
            maximumAttempts: 2,
            lockoutDuration: TimeSpan.FromMinutes(15));

        Assert.False(user.IsLockedOut(firstAttempt));

        var secondAttempt = firstAttempt.AddMinutes(1);

        user.RecordFailedLogin(
            secondAttempt,
            maximumAttempts: 2,
            lockoutDuration: TimeSpan.FromMinutes(15));

        Assert.Equal(2, user.FailedLoginAttempts);
        Assert.Equal(
            secondAttempt.AddMinutes(15),
            user.LockoutEndUtc);
        Assert.True(user.IsLockedOut(secondAttempt));
    }

    [Fact]
    public void RecordSuccessfulLogin_ClearsFailuresAndLockout()
    {
        var user = CreateUser();
        var failedAt = new DateTimeOffset(
            2026, 7, 30, 10, 0, 0, TimeSpan.Zero);

        user.RecordFailedLogin(
            failedAt,
            maximumAttempts: 1,
            lockoutDuration: TimeSpan.FromMinutes(15));

        var successfulAt = failedAt.AddMinutes(5);

        user.RecordSuccessfulLogin(successfulAt);

        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LockoutEndUtc);
        Assert.Equal(successfulAt, user.LastLoginAtUtc);
        Assert.Equal(successfulAt, user.UpdatedAtUtc);
    }

    [Fact]
    public void ChangePasswordHash_UpdatesHashAndTimestamp()
    {
        var user = CreateUser();
        var changedAt = new DateTimeOffset(
            2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

        user.ChangePasswordHash(
            "new-secure-password-hash",
            changedAt);

        Assert.Equal(
            "new-secure-password-hash",
            user.PasswordHash);
        Assert.Equal(changedAt, user.UpdatedAtUtc);
    }

    [Fact]
    public void Deactivate_DisablesAccount()
    {
        var user = CreateUser();
        var deactivatedAt = new DateTimeOffset(
            2026, 7, 30, 13, 0, 0, TimeSpan.Zero);

        user.Deactivate(deactivatedAt);

        Assert.False(user.IsActive);
        Assert.Equal(deactivatedAt, user.UpdatedAtUtc);
    }

    private static UserAccount CreateUser()
    {
        return UserAccount.Register(
            "alwande.demo@securepay.local",
            "Alwande",
            "Demo",
            _ => "secure-password-hash");
    }
}