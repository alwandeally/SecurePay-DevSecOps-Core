using SecurePay.Identity.Api.Contracts.Authentication;

namespace SecurePay.Identity.Api.Application.Authentication;

public interface IUserRegistrationService
{
    Task<RegisterUserResponse> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken);
}
