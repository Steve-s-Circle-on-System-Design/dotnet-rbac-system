using Microsoft.Extensions.DependencyInjection;
using RbacSystem.Application.Features.Auth.Login;
using RbacSystem.Application.Features.Auth.Register;

namespace RbacSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        _ = services.AddScoped<IRegisterUserService, RegisterUserService>();
        _ = services.AddScoped<ILoginService, LoginService>();

        // Injected rather than calling DateTime.UtcNow directly, so lockout expiry
        // and last-login timestamps can be driven deterministically in tests.
        _ = services.AddSingleton(TimeProvider.System);

        return services;
    }
}
