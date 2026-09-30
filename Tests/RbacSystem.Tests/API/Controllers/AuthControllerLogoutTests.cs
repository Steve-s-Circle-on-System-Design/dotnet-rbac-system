using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RbacSystem.API.Controllers;
using RbacSystem.Application.Features.Auth.Login;
using RbacSystem.Application.Features.Auth.Logout;
using RbacSystem.Application.Features.Auth.Refresh;
using RbacSystem.Application.Features.Auth.Register;

namespace RbacSystem.Tests.API.Controllers;

/// <summary>HTTP mapping tests for the logout endpoint.</summary>
public sealed class AuthControllerLogoutTests
{
    private readonly RecordingLogoutService logoutService = new();

    private AuthController CreateController()
    {
        return new AuthController(
            new UnusedRegisterService(),
            new UnusedLoginService(),
            new UnusedRefreshService(),
            logoutService)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task Logout_ReturnsNoContentAndForwardsTheRequest()
    {
        using var cancellation = new CancellationTokenSource();
        LogoutRequest request = new() { RefreshToken = "raw-refresh-token" };

        IActionResult result = await CreateController().Logout(request, cancellation.Token);

        _ = Assert.IsType<NoContentResult>(result);
        (LogoutRequest actualRequest, CancellationToken token) = Assert.Single(logoutService.Calls);
        Assert.Same(request, actualRequest);
        Assert.Equal(cancellation.Token, token);
    }

    private sealed class RecordingLogoutService : ILogoutService
    {
        public List<(LogoutRequest Request, CancellationToken Token)> Calls { get; } = [];

        public Task LogoutAsync(
            LogoutRequest request,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((request, cancellationToken));
            return Task.CompletedTask;
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

    private sealed class UnusedRefreshService : IRefreshService
    {
        public Task<RefreshResult> RefreshAsync(
            RefreshRequest request,
            string? userAgent = null,
            IPAddress? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
