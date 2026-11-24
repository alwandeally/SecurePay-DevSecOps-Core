using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using SecurePay.Transactions.Api.Authorization;
using SecurePay.Transactions.Api.Controllers;

namespace SecurePay.Transactions.UnitTests.Controllers;

public sealed class OperationsTransactionsControllerTests
{
    [Fact]
    public void Controller_RequiresTransactionProcessorPolicy()
    {
        var authorizeAttribute =
            Assert.Single(
                typeof(OperationsTransactionsController)
                    .GetCustomAttributes<
                        AuthorizeAttribute>(
                        inherit: true));

        Assert.Equal(
            AuthorizationPolicies.TransactionProcessor,
            authorizeAttribute.Policy);

        Assert.Empty(
            typeof(OperationsTransactionsController)
                .GetCustomAttributes<
                    AllowAnonymousAttribute>(
                    inherit: true));
    }
}
