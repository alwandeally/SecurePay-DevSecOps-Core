using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecurePay.Transactions.Api.Application.Transactions;
using SecurePay.Transactions.Api.Authorization;
using SecurePay.Transactions.Api.Configuration;
using SecurePay.Transactions.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);

var transactionsConnectionString =
    builder.Configuration.GetConnectionString("TransactionsDatabase")
    ?? throw new InvalidOperationException(
        "The TransactionsDatabase connection string is missing.");

var jwtSection =
    builder.Configuration.GetSection(JwtOptions.SectionName);

var jwtOptions =
    jwtSection.Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "The JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
    string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
    string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
{
    throw new InvalidOperationException(
        "The JWT issuer, audience, and signing key must be configured.");
}

byte[] jwtSigningKeyBytes;

try
{
    // Identity API uses Base64 decoding, so Transactions must do the same.
    jwtSigningKeyBytes =
        Convert.FromBase64String(jwtOptions.SigningKey);
}
catch (FormatException exception)
{
    throw new InvalidOperationException(
        "The JWT signing key must be a valid Base64 string.",
        exception);
}

if (jwtSigningKeyBytes.Length < 32)
{
    throw new InvalidOperationException(
        "The JWT signing key must contain at least 32 bytes.");
}

builder.Services.Configure<JwtOptions>(jwtSection);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

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
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.SaveToken = false;

        options.IncludeErrorDetails =
            builder.Environment.IsDevelopment();

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(jwtSigningKeyBytes),

                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,

                ClockSkew = TimeSpan.FromSeconds(30),

                NameClaimType =
                    JwtRegisteredClaimNames.Sub,

                RoleClaimType = "role"
            };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger =
                    context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("SecurePay.JwtAuthentication");

                logger.LogWarning(
                    context.Exception,
                    "JWT authentication failed.");

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthorizationPolicies.TransactionProcessor,
        policy => policy.RequireRole(
            AuthorizationPolicies.OperationsRole,
            AuthorizationPolicies.AdministratorRole));
});

builder.Services.AddScoped<
    ITransactionService,
    TransactionService>();

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

app.UseAuthentication();
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
