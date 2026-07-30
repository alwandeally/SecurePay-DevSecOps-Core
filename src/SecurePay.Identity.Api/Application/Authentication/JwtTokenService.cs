using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SecurePay.Identity.Api.Configuration;
using SecurePay.Identity.Api.Contracts.Authentication;
using SecurePay.Identity.Api.Domain.Entities;

namespace SecurePay.Identity.Api.Application.Authentication;

public sealed class JwtTokenService(
    IOptions<JwtOptions> jwtOptions)
    : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public LoginResponse CreateAccessToken(UserAccount user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var issuedAtUtc = DateTimeOffset.UtcNow;
        var expiresAtUtc = issuedAtUtc.AddMinutes(
            _jwtOptions.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()),

            new(
                JwtRegisteredClaimNames.Email,
                user.Email),

            new(
                JwtRegisteredClaimNames.GivenName,
                user.FirstName),

            new(
                JwtRegisteredClaimNames.FamilyName,
                user.LastName),

            new(
                "role",
                user.Role.ToString())
        };

        var signingKey = new SymmetricSecurityKey(
            Convert.FromBase64String(_jwtOptions.SigningKey));

        var signingCredentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: issuedAtUtc.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: signingCredentials);

        var accessToken = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return new LoginResponse(
            accessToken,
            "Bearer",
            expiresAtUtc);
    }
}
