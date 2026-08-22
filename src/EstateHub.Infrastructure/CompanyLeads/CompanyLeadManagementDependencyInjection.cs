using EstateHub.Application.CompanyLeads;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.CompanyLeads;

public static class CompanyLeadManagementDependencyInjection
{
    public static IServiceCollection AddInfrastructureCompanyLeadManagement(this IServiceCollection services)
    {
        services.AddScoped<ICompanyLeadManagementService, CompanyLeadManagementService>();
        return services;
    }
}
