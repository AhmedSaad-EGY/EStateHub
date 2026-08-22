using EstateHub.Application.CompanyViewingSlots;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.CompanyViewingSlots;

public static class CompanyViewingSlotManagementDependencyInjection
{
    public static IServiceCollection AddInfrastructureCompanyViewingSlotManagement(this IServiceCollection services)
    {
        services.AddScoped<ICompanyViewingSlotManagementService, CompanyViewingSlotManagementService>();
        return services;
    }
}
