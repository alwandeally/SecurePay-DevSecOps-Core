using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SecurePay.Transactions.IntegrationTests.Infrastructure;

public sealed class TransactionsApiFactory
    : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?>
        originalEnvironmentVariables = [];

    private bool environmentRestored;

    public TransactionsApiFactory(
        string databaseConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            databaseConnectionString);

        SetTemporaryEnvironmentVariable(
            "ConnectionStrings__TransactionsDatabase",
            databaseConnectionString);

        SetTemporaryEnvironmentVariable(
            "Jwt__Issuer",
            TestJwtTokens.Issuer);

        SetTemporaryEnvironmentVariable(
            "Jwt__Audience",
            TestJwtTokens.Audience);

        SetTemporaryEnvironmentVariable(
            "Jwt__SigningKey",
            TestJwtTokens.SigningKey);
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        finally
        {
            RestoreEnvironmentVariables();
        }
    }

    private void SetTemporaryEnvironmentVariable(
        string name,
        string value)
    {
        originalEnvironmentVariables[name] =
            Environment.GetEnvironmentVariable(name);

        Environment.SetEnvironmentVariable(name, value);
    }

    private void RestoreEnvironmentVariables()
    {
        if (environmentRestored)
        {
            return;
        }

        foreach (var setting in originalEnvironmentVariables)
        {
            Environment.SetEnvironmentVariable(
                setting.Key,
                setting.Value);
        }

        environmentRestored = true;
    }
}
