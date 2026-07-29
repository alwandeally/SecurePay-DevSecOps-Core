using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SecurePay.Identity.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);

var identityConnectionString =
    builder.Configuration.GetConnectionString("IdentityDatabase")
    ?? throw new InvalidOperationException(
        "The Identity database connection string is not configured.");

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(
        identityConnectionString,
        npgsqlOptions =>
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null)));

builder.Services.AddHealthChecks()
    .AddCheck(
        "identity-api",
        () => HealthCheckResult.Healthy(),
        tags: ["live"])
    .AddDbContextCheck<IdentityDbContext>(
        name: "identity-database",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration =>
        registration.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration =>
        registration.Tags.Contains("ready")
});

app.MapGet("/", () => Results.Ok(new
{
    service = "SecurePay Identity API",
    status = "Running",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTimeOffset.UtcNow
}))
.WithName("GetIdentityServiceStatus");

app.Run();