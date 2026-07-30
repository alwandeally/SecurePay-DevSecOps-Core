using SecurePay.Identity.Api.Contracts.Authentication;
using SecurePay.Identity.Api.Domain.Entities;

namespace SecurePay.Identity.Api.Application.Authentication;

public interface IJwtTokenService
{
    LoginResponse CreateAccessToken(UserAccount user);
}
