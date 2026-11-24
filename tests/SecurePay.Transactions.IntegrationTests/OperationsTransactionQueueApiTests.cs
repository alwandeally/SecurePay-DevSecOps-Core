using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SecurePay.Transactions.Api.Contracts.Operations;
using SecurePay.Transactions.Api.Domain.Entities;
using SecurePay.Transactions.Api.Domain.Enums;
using SecurePay.Transactions.Api.Persistence;
using SecurePay.Transactions.IntegrationTests.Infrastructure;

namespace SecurePay.Transactions.IntegrationTests;

[Collection(TransactionsApiCollection.Name)]
public sealed class OperationsTransactionQueueApiTests(
    TransactionsApiFixture fixture)
{
    [Fact]
    public async Task GetQueue_WithoutToken_ReturnsUnauthorized()
    {
        using var response =
            await fixture.Client.GetAsync(
                "/api/operations/transactions");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetQueue_WithCustomerToken_ReturnsForbidden()
    {
        using var request = CreateAuthorizedRequest(
            "/api/operations/transactions",
            role: "Customer");

        using var response =
            await fixture.Client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task GetQueue_WithOperationsToken_ReturnsFilteredPage()
    {
        var operationsUserId = Guid.NewGuid();
        var customerUserId = Guid.NewGuid();

        await SeedPendingDepositsAsync(
            customerUserId,
            count: 3);

        var requestUri =
            "/api/operations/transactions" +
            $"?status=Pending" +
            $"&transactionType=Deposit" +
            $"&userId={customerUserId}" +
            $"&page=1" +
            $"&pageSize=2";

        using var request = CreateAuthorizedRequest(
            requestUri,
            role: "Operations",
            userId: operationsUserId);

        using var response =
            await fixture.Client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content.ReadFromJsonAsync<
                PagedOperationsTransactionResponse>();

        Assert.NotNull(body);
        Assert.Equal(1, body.Page);
        Assert.Equal(2, body.PageSize);
        Assert.Equal(3, body.TotalCount);
        Assert.Equal(2, body.TotalPages);
        Assert.Equal(2, body.Items.Count);

        Assert.All(
            body.Items,
            item =>
            {
                Assert.Equal(customerUserId, item.UserId);
                Assert.NotEqual(
                    operationsUserId,
                    item.UserId);
                Assert.Equal(
                    "Deposit",
                    item.TransactionType);
                Assert.Equal("Pending", item.Status);
            });

        var expectedOrder =
            body.Items
                .OrderByDescending(item => item.CreatedAtUtc)
                .ThenByDescending(item => item.TransactionId)
                .Select(item => item.TransactionId);

        Assert.Equal(
            expectedOrder,
            body.Items.Select(item => item.TransactionId));
    }

    private async Task SeedPendingDepositsAsync(
        Guid userId,
        int count)
    {
        using var scope =
            fixture.Factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        var transactions =
            Enumerable.Range(0, count)
                .Select(index =>
                    PaymentTransaction.Create(
                        userId,
                        TransactionType.Deposit,
                        100m + index,
                        "ZAR",
                        $"operations-queue-{Guid.NewGuid():N}",
                        $"Operations queue test {index + 1}"))
                .ToArray();

        dbContext.PaymentTransactions.AddRange(transactions);
        await dbContext.SaveChangesAsync();
    }

    private static HttpRequestMessage CreateAuthorizedRequest(
        string requestUri,
        string role,
        Guid? userId = null)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                requestUri);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                TestJwtTokens.Create(
                    userId ?? Guid.NewGuid(),
                    role));

        return request;
    }
}