using Microsoft.Extensions.DependencyInjection;
using RbacSystem.Application.Features.Auth.Register;

namespace RbacSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        _ = services.AddScoped<IRegisterUserService, RegisterUserService>();

        return services;
    }
}
