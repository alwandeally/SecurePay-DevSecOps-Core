using SecurePay.Identity.Api.Contracts.Authentication;

namespace SecurePay.Identity.Api.Application.Authentication;

public interface IUserLoginService
{
    Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);
}