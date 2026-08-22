using EstateHub.Application.CompanyApplications;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.CompanyApplications;

public static class CompanyApplicationDependencyInjection
{
    public static IServiceCollection AddInfrastructureCompanyApplications(
        this IServiceCollection services)
    {
        services.AddScoped<ICompanyApplicationService, CompanyApplicationService>();
        return services;
    }
}
