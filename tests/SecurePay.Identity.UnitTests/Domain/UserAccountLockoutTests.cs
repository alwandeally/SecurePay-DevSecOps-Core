using SecurePay.Identity.Api.Domain.Entities;

namespace SecurePay.Identity.UnitTests.Domain;

public sealed class UserAccountLockoutTests
{
    [Fact]
    public void RecordFailedLogin_LocksAccountAtMaximumAttempts()
    {
        var user = CreateUser();
        var occurredAtUtc = DateTimeOffset.UtcNow;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            user.RecordFailedLogin(
                occurredAtUtc.AddSeconds(attempt),
                maximumAttempts: 5,
                lockoutDuration: TimeSpan.FromMinutes(15));
        }

        Assert.Equal(5, user.FailedLoginAttempts);
        Assert.NotNull(user.LockoutEndUtc);
        Assert.True(user.IsLockedOut(
            occurredAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void RecordFailedLogin_AfterExpiredLockout_StartsFreshCounter()
    {
        var user = CreateUser();
        var occurredAtUtc = DateTimeOffset.UtcNow;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            user.RecordFailedLogin(
                occurredAtUtc.AddSeconds(attempt),
                maximumAttempts: 5,
                lockoutDuration: TimeSpan.FromMinutes(15));
        }

        var afterLockoutUtc =
            occurredAtUtc.AddMinutes(16);

        user.RecordFailedLogin(
            afterLockoutUtc,
            maximumAttempts: 5,
            lockoutDuration: TimeSpan.FromMinutes(15));

        Assert.Equal(1, user.FailedLoginAttempts);
        Assert.Null(user.LockoutEndUtc);
        Assert.False(user.IsLockedOut(afterLockoutUtc));
    }

    private static UserAccount CreateUser()
    {
        return UserAccount.Register(
            "lockout.test@securepay.local",
            "Lockout",
            "Test",
            _ => "secure-test-password-hash");
    }
}