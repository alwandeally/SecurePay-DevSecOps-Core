namespace SecurePay.Identity.Api.Application.Authentication;

public sealed class InvalidCredentialsException()
    : Exception("The supplied credentials are invalid.");