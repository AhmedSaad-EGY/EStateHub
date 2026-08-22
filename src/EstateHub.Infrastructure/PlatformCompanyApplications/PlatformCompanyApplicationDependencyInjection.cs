using EstateHub.Application.PlatformCompanyApplications;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.PlatformCompanyApplications;

public static class PlatformCompanyApplicationDependencyInjection
{
    public static IServiceCollection AddInfrastructurePlatformCompanyApplications(
        this IServiceCollection services)
    {
        services.AddScoped<IPlatformCompanyApplicationService, PlatformCompanyApplicationService>();
        return services;
    }
}
