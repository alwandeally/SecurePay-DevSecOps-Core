namespace SecurePay.Identity.Api.Application.Authentication;

public sealed class DuplicateEmailException : Exception
{
    public DuplicateEmailException(string email)
        : base($"An account with the email '{email}' already exists.")
    {
    }

    public DuplicateEmailException(
        string email,
        Exception innerException)
        : base(
            $"An account with the email '{email}' already exists.",
            innerException)
    {
    }
}