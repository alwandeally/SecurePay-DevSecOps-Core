using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SecurePay.Identity.Api.Application.Authentication;
using SecurePay.Identity.Api.Configuration;
using SecurePay.Identity.Api.Domain.Entities;

namespace SecurePay.Identity.UnitTests.Application.Authentication;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void CreateAccessToken_CreatesValidSignedToken()
    {
        var signingKeyBytes = Enumerable
            .Range(1, 64)
            .Select(number => (byte)number)
            .ToArray();

        var signingKey = Convert.ToBase64String(
            signingKeyBytes);

        var options = Options.Create(new JwtOptions
        {
            Issuer = "SecurePay.Identity.Api.Tests",
            Audience = "SecurePay.Services.Tests",
            SigningKey = signingKey,
            AccessTokenMinutes = 15
        });

        var tokenService = new JwtTokenService(options);

        var user = UserAccount.Register(
            "alwande.test@securepay.local",
            "Alwande",
            "Test",
            _ => "secure-test-password-hash");

        var response = tokenService.CreateAccessToken(user);

        var validationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = options.Value.Issuer,

                ValidateAudience = true,
                ValidAudience = options.Value.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        signingKeyBytes),

                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,

                ValidAlgorithms =
                [
                    SecurityAlgorithms.HmacSha256
                ],

                ClockSkew = TimeSpan.Zero
            };

        var tokenHandler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false
        };

        var principal = tokenHandler.ValidateToken(
            response.AccessToken,
            validationParameters,
            out var validatedToken);

        var jwt = Assert.IsType<JwtSecurityToken>(
            validatedToken);

        Assert.Equal("Bearer", response.TokenType);

        Assert.Equal(
            SecurityAlgorithms.HmacSha256,
            jwt.Header.Alg);

        Assert.Equal(
            user.Id.ToString(),
            principal.FindFirst(
                JwtRegisteredClaimNames.Sub)?.Value);

        Assert.Equal(
            user.Email,
            principal.FindFirst(
                JwtRegisteredClaimNames.Email)?.Value);

        Assert.Equal(
            user.FirstName,
            principal.FindFirst(
                JwtRegisteredClaimNames.GivenName)?.Value);

        Assert.Equal(
            user.LastName,
            principal.FindFirst(
                JwtRegisteredClaimNames.FamilyName)?.Value);

        Assert.Equal(
            user.Role.ToString(),
            principal.FindFirst("role")?.Value);

        Assert.NotNull(
            principal.FindFirst(
                JwtRegisteredClaimNames.Jti));

        Assert.True(
            response.ExpiresAtUtc >
            DateTimeOffset.UtcNow);
    }
}