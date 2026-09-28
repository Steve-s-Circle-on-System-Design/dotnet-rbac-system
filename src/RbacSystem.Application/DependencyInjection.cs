using Microsoft.Extensions.DependencyInjection;
using RbacSystem.Application.Features.Auth.AccessToken;
using RbacSystem.Application.Features.Auth.Login;
using RbacSystem.Application.Features.Auth.Logout;
using RbacSystem.Application.Features.Auth.Refresh;
using RbacSystem.Application.Features.Auth.Register;

namespace RbacSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        _ = services.AddScoped<IAccessTokenValidator, AccessTokenValidator>();
        _ = services.AddScoped<IRegisterUserService, RegisterUserService>();
        _ = services.AddScoped<ILoginService, LoginService>();
        _ = services.AddScoped<ILogoutService, LogoutService>();
        _ = services.AddScoped<IRefreshService, RefreshService>();

        // Injected rather than calling DateTime.UtcNow directly, so lockout expiry
        // and last-login timestamps can be driven deterministically in tests.
        _ = services.AddSingleton(TimeProvider.System);

        return services;
    }
}
