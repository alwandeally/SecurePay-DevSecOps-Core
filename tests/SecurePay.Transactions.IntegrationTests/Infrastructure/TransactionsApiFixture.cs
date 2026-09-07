using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecurePay.Transactions.Api.Persistence;
using Testcontainers.PostgreSql;

namespace SecurePay.Transactions.IntegrationTests.Infrastructure;

public sealed class TransactionsApiFixture
    : IAsyncLifetime
{
    private readonly PostgreSqlContainer database =
        new PostgreSqlBuilder("postgres:17.10-alpine3.24")
            .WithDatabase("securepay_transactions_tests")
            .WithUsername("securepay_test_user")
            .WithPassword(
                "SecurePay-Test-Only-Password-2026!")
            .Build();

    public TransactionsApiFactory Factory { get; private set; } =
        null!;

    public HttpClient Client { get; private set; } =
        null!;

    public async Task InitializeAsync()
    {
        await database.StartAsync();

        Factory =
            new TransactionsApiFactory(
                database.GetConnectionString());

        using var scope =
            Factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        await dbContext.Database.MigrateAsync();

        Client =
            Factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress =
                        new Uri("https://localhost"),
                    AllowAutoRedirect = false
                });
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();

        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        await database.DisposeAsync();
    }
}
