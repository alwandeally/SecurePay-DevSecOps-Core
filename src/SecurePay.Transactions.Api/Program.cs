using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using SecurePay.Transactions.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);

var transactionsConnectionString =
    builder.Configuration.GetConnectionString(
        "TransactionsDatabase")
    ?? throw new InvalidOperationException(
        "The TransactionsDatabase connection string is missing.");

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();

builder.Services.AddDbContext<TransactionsDbContext>(
    options =>
        options.UseNpgsql(
            transactionsConnectionString,
            postgresqlOptions =>
                postgresqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null)));

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<TransactionsDbContext>(
        name: "transactions-database",
        tags: new[] { "ready" });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false
    });

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = healthCheck =>
            healthCheck.Tags.Contains("ready")
    });

app.MapGet(
    "/",
    () => Results.Ok(new
    {
        service = "SecurePay Transactions API",
        status = "Running",
        environment = app.Environment.EnvironmentName,
        timestamp = DateTimeOffset.UtcNow
    }))
    .WithName("GetTransactionsServiceStatus");

app.Run();
