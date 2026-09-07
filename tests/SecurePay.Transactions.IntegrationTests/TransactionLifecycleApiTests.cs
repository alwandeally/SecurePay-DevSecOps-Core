using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecurePay.Transactions.Api.Contracts.Transactions;
using SecurePay.Transactions.Api.Domain.Entities;
using SecurePay.Transactions.Api.Domain.Enums;
using SecurePay.Transactions.Api.Persistence;
using SecurePay.Transactions.IntegrationTests.Infrastructure;

namespace SecurePay.Transactions.IntegrationTests;

[Collection(TransactionsApiCollection.Name)]
public sealed class TransactionLifecycleApiTests(
    TransactionsApiFixture fixture)
{
    [Fact]
    public async Task Complete_WithoutToken_ReturnsUnauthorized()
    {
        var transaction =
            await SeedPendingTransactionAsync();

        using var response =
            await fixture.Client.PostAsync(
                $"/api/operations/transactions/{transaction.Id}/complete",
                content: null);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Complete_WithCustomerToken_ReturnsForbidden()
    {
        var transaction =
            await SeedPendingTransactionAsync();

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/operations/transactions/{transaction.Id}/complete",
            role: "Customer");

        using var response =
            await fixture.Client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Complete_WithOperationsToken_PersistsCompletedStatus()
    {
        var transaction =
            await SeedPendingTransactionAsync();

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/operations/transactions/{transaction.Id}/complete",
            role: "Operations");

        using var response =
            await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<TransactionResponse>();

        Assert.NotNull(body);
        Assert.Equal("Completed", body.Status);
        Assert.NotNull(body.CompletedAtUtc);

        var persisted =
            await LoadTransactionAsync(transaction.Id);

        Assert.Equal(
            TransactionStatus.Completed,
            persisted.Status);
        Assert.NotNull(persisted.CompletedAtUtc);
    }

    [Fact]
    public async Task Fail_WithAdministratorToken_PersistsFailureReason()
    {
        var transaction =
            await SeedPendingTransactionAsync();

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/operations/transactions/{transaction.Id}/fail",
            role: "Administrator",
            content: JsonContent.Create(new
            {
                failureReason =
                    "Payment provider rejected the transaction"
            }));

        using var response =
            await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<TransactionResponse>();

        Assert.NotNull(body);
        Assert.Equal("Failed", body.Status);
        Assert.Equal(
            "Payment provider rejected the transaction",
            body.FailureReason);

        var persisted =
            await LoadTransactionAsync(transaction.Id);

        Assert.Equal(
            TransactionStatus.Failed,
            persisted.Status);
        Assert.Equal(
            "Payment provider rejected the transaction",
            persisted.FailureReason);
    }

    [Fact]
    public async Task Complete_WhenTransactionIsMissing_ReturnsNotFound()
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/operations/transactions/{Guid.NewGuid()}/complete",
            role: "Operations");

        using var response =
            await fixture.Client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Fail_WhenAlreadyCompleted_ReturnsConflict()
    {
        var transaction =
            await SeedPendingTransactionAsync();

        using var completeRequest = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/operations/transactions/{transaction.Id}/complete",
            role: "Operations");

        using var completeResponse =
            await fixture.Client.SendAsync(completeRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            completeResponse.StatusCode);

        using var failRequest = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/operations/transactions/{transaction.Id}/fail",
            role: "Operations",
            content: JsonContent.Create(new
            {
                failureReason =
                    "This transition must be rejected"
            }));

        using var failResponse =
            await fixture.Client.SendAsync(failRequest);

        Assert.Equal(
            HttpStatusCode.Conflict,
            failResponse.StatusCode);
    }

    private async Task<PaymentTransaction>
        SeedPendingTransactionAsync()
    {
        using var scope =
            fixture.Factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        var transaction =
            PaymentTransaction.Create(
                Guid.NewGuid(),
                TransactionType.Deposit,
                250m,
                "ZAR",
                $"integration-{Guid.NewGuid():N}",
                "Integration test transaction");

        dbContext.PaymentTransactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        return transaction;
    }

    private async Task<PaymentTransaction> LoadTransactionAsync(
        Guid transactionId)
    {
        using var scope =
            fixture.Factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<TransactionsDbContext>();

        return await dbContext.PaymentTransactions
            .AsNoTracking()
            .SingleAsync(
                transaction =>
                    transaction.Id == transactionId);
    }

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string requestUri,
        string role,
        HttpContent? content = null)
    {
        var request =
            new HttpRequestMessage(method, requestUri)
            {
                Content = content
            };

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                TestJwtTokens.Create(
                    Guid.NewGuid(),
                    role));

        return request;
    }
}
