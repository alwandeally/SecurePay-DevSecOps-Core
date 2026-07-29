using SecurePay.Identity.Api.Domain.Enums;

namespace SecurePay.Identity.Api.Domain.Entities;

public sealed class UserAccount
{
    private UserAccount()
    {
    }

    public UserAccount(
        string email,
        string passwordHash,
        string firstName,
        string lastName)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));

        Id = Guid.NewGuid();
        Email = email.Trim();
        NormalizedEmail = email.Trim().ToUpperInvariant();
        PasswordHash = passwordHash;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Role = UserRole.Customer;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public int FailedLoginAttempts { get; private set; }

    public DateTimeOffset? LockoutEndUtc { get; private set; }

    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public bool IsLockedOut(DateTimeOffset currentTimeUtc)
    {
        return LockoutEndUtc.HasValue &&
               LockoutEndUtc.Value > currentTimeUtc;
    }

    public void RecordFailedLogin(
        DateTimeOffset occurredAtUtc,
        int maximumAttempts,
        TimeSpan lockoutDuration)
    {
        if (maximumAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumAttempts));

        FailedLoginAttempts++;

        if (FailedLoginAttempts >= maximumAttempts)
            LockoutEndUtc = occurredAtUtc.Add(lockoutDuration);

        UpdatedAtUtc = occurredAtUtc;
    }

    public void RecordSuccessfulLogin(DateTimeOffset occurredAtUtc)
    {
        FailedLoginAttempts = 0;
        LockoutEndUtc = null;
        LastLoginAtUtc = occurredAtUtc;
        UpdatedAtUtc = occurredAtUtc;
    }

    public void ChangePasswordHash(
        string passwordHash,
        DateTimeOffset changedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        PasswordHash = passwordHash;
        UpdatedAtUtc = changedAtUtc;
    }

    public void Deactivate(DateTimeOffset deactivatedAtUtc)
    {
        IsActive = false;
        UpdatedAtUtc = deactivatedAtUtc;
    }
}
