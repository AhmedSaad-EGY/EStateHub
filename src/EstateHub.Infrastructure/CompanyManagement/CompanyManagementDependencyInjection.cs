using EstateHub.Application.CompanyManagement;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.CompanyManagement;

public static class CompanyManagementDependencyInjection
{
    public static IServiceCollection AddInfrastructureCompanyManagement(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICompanyManagementService, CompanyManagementService>();

        return services;
    }
}
