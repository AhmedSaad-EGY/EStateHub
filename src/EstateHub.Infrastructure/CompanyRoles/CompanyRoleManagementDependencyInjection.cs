using EstateHub.Application.CompanyRoles;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.CompanyRoles;

public static class CompanyRoleManagementDependencyInjection
{
    public static IServiceCollection AddInfrastructureCompanyRoles(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICompanyRoleManagementService, CompanyRoleManagementService>();

        return services;
    }
}
