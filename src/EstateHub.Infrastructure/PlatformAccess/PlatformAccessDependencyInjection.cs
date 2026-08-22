using EstateHub.Application.PlatformAccess;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.PlatformAccess;

public static class PlatformAccessDependencyInjection
{
    public static IServiceCollection AddInfrastructurePlatformAccess(
        this IServiceCollection services)
    {
        services.AddScoped<IPlatformAccessService, PlatformAccessService>();
        return services;
    }
}

