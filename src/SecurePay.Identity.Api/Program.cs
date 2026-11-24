using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using SecurePay.Identity.Api.Application.Authentication;
using SecurePay.Identity.Api.Application.Bootstrap;
using SecurePay.Identity.Api.Configuration;
using SecurePay.Identity.Api.Domain.Entities;
using SecurePay.Identity.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);

var identityConnectionString =
    builder.Configuration.GetConnectionString(
        "IdentityDatabase")
    ?? throw new InvalidOperationException(
        "The Identity database connection string " +
        "is not configured.");

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();

builder.Services.AddSingleton<
    IJwtTokenService,
    JwtTokenService>();

builder.Services.AddScoped<
    IUserRegistrationService,
    UserRegistrationService>();

builder.Services.AddScoped<
    IUserLoginService,
    UserLoginService>();

builder.Services.AddScoped<
    IPasswordHasher<UserAccount>,
    PasswordHasher<UserAccount>>();

builder.Services.AddScoped<
    IOperationsAccountBootstrapper,
    OperationsAccountBootstrapper>();

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

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(
        JwtOptions.SectionName))
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.Issuer),
        "JWT issuer is required.")
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.Audience),
        "JWT audience is required.")
    .Validate(
        options => options.SigningKey.Length >= 64,
        "JWT signing key must contain at least " +
        "64 characters.")
    .Validate(
        options =>
            options.AccessTokenMinutes is >= 5 and <= 60,
        "JWT access-token lifetime must be between " +
        "5 and 60 minutes.")
    .ValidateOnStart();

builder.Services
    .AddOptions<OperationsBootstrapOptions>()
    .Bind(builder.Configuration.GetSection(
        OperationsBootstrapOptions.SectionName))
    .Validate(
        options =>
            !options.Enabled ||
            !string.IsNullOrWhiteSpace(options.Email),
        "Operations bootstrap email is required.")
    .Validate(
        options =>
            !options.Enabled ||
            new EmailAddressAttribute()
                .IsValid(options.Email),
        "Operations bootstrap email is invalid.")
    .Validate(
        options =>
            !options.Enabled ||
            (!string.IsNullOrWhiteSpace(options.Password) &&
             options.Password.Length >= 16),
        "Operations bootstrap password must contain " +
        "at least 16 characters.")
    .Validate(
        options =>
            !options.Enabled ||
            !string.IsNullOrWhiteSpace(options.FirstName),
        "Operations bootstrap first name is required.")
    .Validate(
        options =>
            !options.Enabled ||
            !string.IsNullOrWhiteSpace(options.LastName),
        "Operations bootstrap last name is required.")
    .ValidateOnStart();

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "JWT configuration is missing.");

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
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
                    new SymmetricSecurityKey(
                        Convert.FromBase64String(
                            jwtOptions.SigningKey)),

                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,

                ValidAlgorithms =
                [
                    SecurityAlgorithms.HmacSha256
                ],

                NameClaimType =
                    JwtRegisteredClaimNames.Sub,
                RoleClaimType = "role",

                ClockSkew =
                    TimeSpan.FromSeconds(30)
            };
    });

var app = builder.Build();

await using (var scope =
    app.Services.CreateAsyncScope())
{
    var bootstrapper = scope.ServiceProvider
        .GetRequiredService<
            IOperationsAccountBootstrapper>();

    await bootstrapper.BootstrapAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = registration =>
            registration.Tags.Contains("live")
    });

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration =>
            registration.Tags.Contains("ready")
    });

app.MapGet("/", () => Results.Ok(new
{
    service = "SecurePay Identity API",
    status = "Running",
    environment =
        app.Environment.EnvironmentName,
    timestamp = DateTimeOffset.UtcNow
}))
.WithName("GetIdentityServiceStatus");

app.Run();
