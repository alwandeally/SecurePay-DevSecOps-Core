using SecurePay.Identity.Api.Domain.Entities;
using SecurePay.Identity.Api.Domain.Enums;
using Xunit;

namespace SecurePay.Identity.UnitTests.Domain;

public sealed class UserAccountOperationsTests
{
    [Fact]
    public void CreateOperations_WithValidDetails_CreatesActiveOperationsAccount()
    {
        var beforeCreation = DateTimeOffset.UtcNow;

        var user = UserAccount.CreateOperations(
            "operations@securepay.local",
            "SecurePay",
            "Operations",
            _ => "secure-operations-password-hash");

        var afterCreation = DateTimeOffset.UtcNow;

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("operations@securepay.local", user.Email);
        Assert.Equal(
            "OPERATIONS@SECUREPAY.LOCAL",
            user.NormalizedEmail);
        Assert.Equal(
            "secure-operations-password-hash",
            user.PasswordHash);
        Assert.Equal("SecurePay", user.FirstName);
        Assert.Equal("Operations", user.LastName);
        Assert.Equal(UserRole.Operations, user.Role);
        Assert.True(user.IsActive);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.InRange(
            user.CreatedAtUtc,
            beforeCreation,
            afterCreation);
    }

    [Fact]
    public void CreateOperations_NormalizesAndTrimsDetails()
    {
        var user = UserAccount.CreateOperations(
            "  Operations@SecurePay.Local  ",
            "  SecurePay  ",
            "  Operations  ",
            _ => "secure-operations-password-hash");

        Assert.Equal(
            "Operations@SecurePay.Local",
            user.Email);
        Assert.Equal(
            "OPERATIONS@SECUREPAY.LOCAL",
            user.NormalizedEmail);
        Assert.Equal("SecurePay", user.FirstName);
        Assert.Equal("Operations", user.LastName);
        Assert.Equal(UserRole.Operations, user.Role);
    }
}
