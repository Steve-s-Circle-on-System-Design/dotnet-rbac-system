using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RbacSystem.API.Controllers;
using RbacSystem.Application.Features.Auth.Login;
using RbacSystem.Application.Features.Auth.Logout;
using RbacSystem.Application.Features.Auth.Refresh;
using RbacSystem.Application.Features.Auth.Register;

namespace RbacSystem.Tests.API.Controllers;

/// <summary>HTTP mapping tests for the refresh endpoint.</summary>
public sealed class AuthControllerRefreshTests
{
    private readonly RecordingRefreshService refreshService = new();

    private AuthController CreateController(string userAgent = "curl/8.0", IPAddress? address = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = userAgent;
        httpContext.Connection.RemoteIpAddress = address ?? IPAddress.Parse("203.0.113.7");

        return new AuthController(
            new UnusedRegisterService(),
            new UnusedLoginService(),
            refreshService,
            new UnusedLogoutService())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    [Fact]
    public async Task Refresh_ReturnsOkWithTheReplacementPair_OnSuccess()
    {
        RefreshResponse response = new("access-token", "refresh-token", "Bearer", 900);
        refreshService.Result = RefreshResult.Success(response);

        IActionResult action = await CreateController().Refresh(
            new RefreshRequest { RefreshToken = "current-raw-token" },
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(action);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task Refresh_ReturnsTheGenericUnauthorizedProblem_OnFailure()
    {
        refreshService.Result = RefreshResult.Invalid();

        IActionResult action = await CreateController().Refresh(
            new RefreshRequest { RefreshToken = "invalid-token" },
            CancellationToken.None);

        ObjectResult unauthorized = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);

        ProblemDetails problem = Assert.IsType<ProblemDetails>(unauthorized.Value);
        Assert.Equal("Refresh failed", problem.Title);
        Assert.Equal("Invalid refresh token", problem.Detail);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.Status);
    }

    [Fact]
    public async Task Refresh_ForwardsRequestMetadataAndCancellation()
    {
        var address = IPAddress.Parse("198.51.100.8");
        using var cancellation = new CancellationTokenSource();
        RefreshRequest request = new() { RefreshToken = "current-raw-token" };

        _ = await CreateController("TestClient/2.0", address)
            .Refresh(request, cancellation.Token);

        (RefreshRequest actualRequest, string? userAgent, IPAddress? ipAddress, CancellationToken token) =
            Assert.Single(refreshService.Calls);

        Assert.Same(request, actualRequest);
        Assert.Equal("TestClient/2.0", userAgent);
        Assert.Equal(address, ipAddress);
        Assert.Equal(cancellation.Token, token);
    }

    private sealed class RecordingRefreshService : IRefreshService
    {
        public RefreshResult Result { get; set; } = RefreshResult.Invalid();

        public List<(RefreshRequest Request, string? UserAgent, IPAddress? IpAddress, CancellationToken Token)> Calls
        {
            get;
        } = [];

        public Task<RefreshResult> RefreshAsync(
            RefreshRequest request,
            string? userAgent = null,
            IPAddress? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((request, userAgent, ipAddress, cancellationToken));
            return Task.FromResult(Result);
        }
    }

    private sealed class UnusedRegisterService : IRegisterUserService
    {
        public Task<RegisterResult> RegisterAsync(
            RegisterRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class UnusedLoginService : ILoginService
    {
        public Task<LoginResult> LoginAsync(
            LoginRequest request,
            string? userAgent = null,
            IPAddress? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class UnusedLogoutService : ILogoutService
    {
        public Task LogoutAsync(
            LogoutRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
