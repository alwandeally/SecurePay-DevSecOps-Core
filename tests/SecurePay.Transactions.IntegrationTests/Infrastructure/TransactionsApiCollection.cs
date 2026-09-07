namespace SecurePay.Transactions.IntegrationTests.Infrastructure;

[CollectionDefinition(
    Name,
    DisableParallelization = true)]
public sealed class TransactionsApiCollection
    : ICollectionFixture<TransactionsApiFixture>
{
    public const string Name =
        "Transactions API integration tests";
}
