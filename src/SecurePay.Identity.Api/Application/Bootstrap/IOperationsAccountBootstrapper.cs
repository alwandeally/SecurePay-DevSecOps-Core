namespace SecurePay.Identity.Api.Application.Bootstrap;

public interface IOperationsAccountBootstrapper
{
    Task BootstrapAsync(
        CancellationToken cancellationToken = default);
}
