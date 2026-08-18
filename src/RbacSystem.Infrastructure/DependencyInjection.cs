using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RbacSystem.Application.Interfaces.Repositories;
using RbacSystem.Application.Interfaces.Services;
using RbacSystem.Infrastructure.Configuration;
using RbacSystem.Infrastructure.Persistence;
using RbacSystem.Infrastructure.Repositories;
using RbacSystem.Infrastructure.Services;

namespace RbacSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "The 'ConnectionStrings:DefaultConnection' setting is required. " +
                "Configure it with .NET user secrets for local development.");

        _ = services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        _ = services.Configure<PasswordHashingOptions>(
            configuration.GetSection(PasswordHashingOptions.SectionName));

        // The API marks these ValidateOnStart, which turns a missing or too-short
        // secret into a startup failure instead of a first-login failure.
        _ = services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations();

        _ = services.AddOptions<AuthTokenOptions>()
            .Bind(configuration.GetSection(AuthTokenOptions.SectionName))
            .ValidateDataAnnotations();

        _ = services.AddScoped<IUserRepository, UserRepository>();
        _ = services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        _ = services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        _ = services.AddSingleton<RefreshTokenHasher>();
        _ = services.AddScoped<ITokenService, JwtTokenService>();
        _ = services.AddScoped<IUserRegisteredEventPublisher, LoggingUserRegisteredEventPublisher>();

        return services;
    }
}
