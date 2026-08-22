using EstateHub.Application.PlatformAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Api.Authorization.Platform;

public static class PlatformAdminAuthorizationDependencyInjection
{
    public static IServiceCollection AddPlatformAdminAuthorization(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                PlatformRoleNames.PlatformAdmin,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PlatformAdminRequirement()));
        });
        services.AddScoped<IAuthorizationHandler, PlatformAdminAuthorizationHandler>();
        return services;
    }
}
