using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RbacSystem.Application;
using RbacSystem.Application.Common.Configuration;
using RbacSystem.Infrastructure;
using RbacSystem.Infrastructure.Configuration;
using RbacSystem.Infrastructure.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RBAC System API",
        Version = "v1",
        Description = "Role-Based Access Control REST API"
    });

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the accessToken returned by /api/auth/login."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            },
            Array.Empty<string>()
        }
    });
});

// Infrastructure binds and annotates these; marking them ValidateOnStart here turns
// that into a startup failure rather than one surfacing on the first login attempt.
builder.Services.AddOptions<JwtOptions>().ValidateOnStart();
builder.Services.AddOptions<AuthTokenOptions>().ValidateOnStart();
builder.Services.AddOptions<AccountLockoutOptions>().ValidateOnStart();

IConfigurationSection jwtSection = builder.Configuration.GetSection("Jwt");

string signingKey = jwtSection["Key"]
    ?? throw new InvalidOperationException(
        "The 'Jwt:Key' setting is required. Configure it with .NET user secrets for local development.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Inbound mapping would rewrite the short "role" and "sub" claims to their
        // long schema URIs during validation, leaving RoleClaimType and NameClaimType
        // below pointing at names that no longer exist. Role checks would then fail
        // silently, which is the worst way for authorization to break.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),

            // Pinned so the handler cannot be talked into accepting a token signed
            // with a different algorithm than the one this service issues.
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],

            // Tokens carry a short "role" claim to match the sibling services, so
            // authorization is pointed at that name instead of the schema URI it
            // would otherwise expect.
            RoleClaimType = JwtTokenService.RoleClaim,
            NameClaimType = JwtRegisteredClaimNames.Sub,

            // Without this the handler allows five minutes of grace, so a token
            // advertised as lasting 15 minutes would really be accepted for 20.
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RBAC System API v1");

        // Served under /swagger, which is what launchSettings.json already opens on
        // startup; hosting it at the root made that configured launch URL 404.
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>
/// Exposes the implicitly generated entry point so integration tests can host the
/// real application through <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program
{
    /// <summary>Prevents the compiler from emitting a default public constructor.</summary>
    protected Program()
    {
    }
}
