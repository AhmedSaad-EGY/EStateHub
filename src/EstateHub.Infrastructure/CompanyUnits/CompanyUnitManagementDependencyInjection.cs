using EstateHub.Application.CompanyUnits;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.CompanyUnits;

public static class CompanyUnitManagementDependencyInjection
{
    public static IServiceCollection AddInfrastructureCompanyUnitManagement(this IServiceCollection services)
    {
        services.AddScoped<ICompanyUnitManagementService, CompanyUnitManagementService>();
        return services;
    }
}
