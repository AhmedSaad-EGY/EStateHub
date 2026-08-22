using EstateHub.Application.CompanyViewingBookings;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.CompanyViewingBookings;

public static class CompanyViewingBookingManagementDependencyInjection
{
    public static IServiceCollection AddInfrastructureCompanyViewingBookingManagement(
        this IServiceCollection services)
    {
        services.AddScoped<
            ICompanyViewingBookingManagementService,
            CompanyViewingBookingManagementService>();
        return services;
    }
}
