namespace SecurePay.Identity.Api.Configuration;

public sealed class OperationsBootstrapOptions
{
    public const string SectionName = "OperationsBootstrap";

    public bool Enabled { get; init; }

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string FirstName { get; init; } = "SecurePay";

    public string LastName { get; init; } = "Operations";
}
