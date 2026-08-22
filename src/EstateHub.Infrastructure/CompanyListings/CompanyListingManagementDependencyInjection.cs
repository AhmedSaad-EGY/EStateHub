using EstateHub.Application.CompanyListings;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.CompanyListings;

public static class CompanyListingManagementDependencyInjection
{
    public static IServiceCollection AddInfrastructureCompanyListingManagement(this IServiceCollection services)
    {
        services.AddScoped<ICompanyListingManagementService, CompanyListingManagementService>();
        return services;
    }
}
