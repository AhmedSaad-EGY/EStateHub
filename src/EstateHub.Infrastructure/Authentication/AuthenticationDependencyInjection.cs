using System.Text;
using EstateHub.Application.Authentication;
using EstateHub.Infrastructure.Authentication.Options;
using EstateHub.Infrastructure.Identity;
using EstateHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EstateHub.Infrastructure.Authentication;

public static class AuthenticationDependencyInjection
{
    private const int MinimumSigningKeyBytes = 32;

    public static IServiceCollection AddInfrastructureAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetRequiredSection(JwtOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Issuer),
                $"{JwtOptions.SectionName}:Issuer is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Audience),
                $"{JwtOptions.SectionName}:Audience is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.SigningKey)
                    && Encoding.UTF8.GetByteCount(options.SigningKey) >= MinimumSigningKeyBytes,
                $"{JwtOptions.SectionName}:SigningKey must contain at least {MinimumSigningKeyBytes} bytes for HMAC SHA-256.")
            .Validate(
                options => options.AccessTokenMinutes > 0,
                $"{JwtOptions.SectionName}:AccessTokenMinutes must be greater than zero.")
            .Validate(
                options => options.RefreshTokenDays > 0,
                $"{JwtOptions.SectionName}:RefreshTokenDays must be greater than zero.")
            .Validate(
                options => options.RememberMeRefreshTokenDays > 0,
                $"{JwtOptions.SectionName}:RememberMeRefreshTokenDays must be greater than zero.")
            .ValidateOnStart();

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.SignIn.RequireConfirmedPhoneNumber = false;

                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 1;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<EstateHubDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IAuthenticationTokenService, AuthenticationTokenService>();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptionsAccessor) =>
            {
                var jwtOptions = jwtOptionsAccessor.Value;

                bearerOptions.MapInboundClaims = false;
                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        return services;
    }
}
