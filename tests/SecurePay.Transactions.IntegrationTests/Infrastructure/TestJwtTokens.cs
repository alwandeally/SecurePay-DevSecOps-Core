using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace SecurePay.Transactions.IntegrationTests.Infrastructure;

public static class TestJwtTokens
{
    public const string Issuer =
        "SecurePay.IntegrationTests";

    public const string Audience =
        "SecurePay.Transactions.IntegrationTests";

    public static string SigningKey { get; } =
        Convert.ToBase64String(
            Enumerable.Repeat((byte)0x42, 64).ToArray());

    public static string Create(
        Guid userId,
        string role)
    {
        var signingCredentials =
            new SigningCredentials(
                new SymmetricSecurityKey(
                    Convert.FromBase64String(SigningKey)),
                SecurityAlgorithms.HmacSha256);

        var token =
            new JwtSecurityToken(
                issuer: Issuer,
                audience: Audience,
                claims:
                [
                    new Claim(
                        JwtRegisteredClaimNames.Sub,
                        userId.ToString()),
                    new Claim(
                        JwtRegisteredClaimNames.Jti,
                        Guid.NewGuid().ToString()),
                    new Claim("role", role)
                ],
                notBefore: DateTime.UtcNow.AddMinutes(-1),
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}
