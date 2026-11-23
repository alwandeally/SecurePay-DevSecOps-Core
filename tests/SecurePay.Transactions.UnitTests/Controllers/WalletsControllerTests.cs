using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecurePay.Transactions.Api.Application.Wallets;
using SecurePay.Transactions.Api.Contracts.Wallets;
using SecurePay.Transactions.Api.Controllers;

namespace SecurePay.Transactions.UnitTests.Controllers;

public sealed class WalletsControllerTests
{
    [Fact]
    public async Task GetAllAsync_WhenUserIsNotAuthenticated_ReturnsUnauthorized()
    {
        var service =
            new FakeWalletQueryService();

        var controller =
            CreateController(service);

        var response =
            await controller.GetAllAsync(
                CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(
            response.Result);

        Assert.Null(service.RequestedUserId);
    }

    [Fact]
    public async Task GetAllAsync_WhenUserIsAuthenticated_ReturnsUserWallets()
    {
        var userId = Guid.NewGuid();

        IReadOnlyList<WalletResponse> wallets =
        [
            new WalletResponse(
                WalletId: Guid.NewGuid(),
                Currency: "ZAR",
                Balance: 250.00m,
                CreatedAtUtc: new DateTimeOffset(
                    2026,
                    11,
                    22,
                    10,
                    0,
                    0,
                    TimeSpan.Zero),
                UpdatedAtUtc: null)
        ];

        var service =
            new FakeWalletQueryService
            {
                Wallets = wallets
            };

        var controller =
            CreateController(
                service,
                userId);

        var response =
            await controller.GetAllAsync(
                CancellationToken.None);

        var okResult =
            Assert.IsType<OkObjectResult>(
                response.Result);

        Assert.Same(
            wallets,
            okResult.Value);

        Assert.Equal(
            (Guid?)userId,
            service.RequestedUserId);
    }

    [Fact]
    public async Task GetByCurrencyAsync_WhenWalletDoesNotExist_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();

        var service =
            new FakeWalletQueryService
            {
                Wallet = null
            };

        var controller =
            CreateController(
                service,
                userId);

        var response =
            await controller.GetByCurrencyAsync(
                "ZAR",
                CancellationToken.None);

        Assert.IsType<NotFoundResult>(
            response.Result);

        Assert.Equal(
            (Guid?)userId,
            service.RequestedUserId);

        Assert.Equal(
            "ZAR",
            service.RequestedCurrency);
    }

    [Fact]
    public async Task GetLedgerEntriesAsync_WhenPaginationIsInvalid_ReturnsBadRequest()
    {
        var userId = Guid.NewGuid();

        var service =
            new FakeWalletQueryService
            {
                LedgerEntriesException =
                    new ArgumentOutOfRangeException(
                        "page",
                        "Page must be greater than zero.")
            };

        var controller =
            CreateController(
                service,
                userId);

        var response =
            await controller.GetLedgerEntriesAsync(
                "ZAR",
                CancellationToken.None,
                page: 0,
                pageSize: 20);

        var badRequest =
            Assert.IsType<BadRequestObjectResult>(
                response.Result);

        var problem =
            Assert.IsType<ProblemDetails>(
                badRequest.Value);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            problem.Status);

        Assert.Equal(
            "Invalid wallet request",
            problem.Title);

        Assert.Equal(
            0,
            service.RequestedPage);

        Assert.Equal(
            20,
            service.RequestedPageSize);
    }

    private static WalletsController CreateController(
        IWalletQueryService service,
        Guid? userId = null)
    {
        var identity =
            userId.HasValue
                ? new ClaimsIdentity(
                    [
                        new Claim(
                            JwtRegisteredClaimNames.Sub,
                            userId.Value.ToString())
                    ],
                    authenticationType: "Test")
                : new ClaimsIdentity();

        var httpContext =
            new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            };

        return new WalletsController(service)
        {
            ControllerContext =
                new ControllerContext
                {
                    HttpContext = httpContext
                }
        };
    }

    private sealed class FakeWalletQueryService
        : IWalletQueryService
    {
        public IReadOnlyList<WalletResponse> Wallets
        {
            get;
            init;
        } = [];

        public WalletResponse? Wallet
        {
            get;
            init;
        }

        public PagedLedgerEntriesResponse? LedgerEntries
        {
            get;
            init;
        }

        public Exception? LedgerEntriesException
        {
            get;
            init;
        }

        public Guid? RequestedUserId
        {
            get;
            private set;
        }

        public string? RequestedCurrency
        {
            get;
            private set;
        }

        public int? RequestedPage
        {
            get;
            private set;
        }

        public int? RequestedPageSize
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<WalletResponse>> GetAllAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            RequestedUserId = userId;

            return Task.FromResult(Wallets);
        }

        public Task<WalletResponse?> GetByCurrencyAsync(
            Guid userId,
            string currency,
            CancellationToken cancellationToken)
        {
            RequestedUserId = userId;
            RequestedCurrency = currency;

            return Task.FromResult(Wallet);
        }

        public Task<PagedLedgerEntriesResponse?>
            GetLedgerEntriesAsync(
                Guid userId,
                string currency,
                int page,
                int pageSize,
                CancellationToken cancellationToken)
        {
            RequestedUserId = userId;
            RequestedCurrency = currency;
            RequestedPage = page;
            RequestedPageSize = pageSize;

            if (LedgerEntriesException is not null)
            {
                throw LedgerEntriesException;
            }

            return Task.FromResult(
                LedgerEntries);
        }
    }
}