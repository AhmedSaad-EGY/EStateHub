using EstateHub.Application.ViewingBookings;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.ViewingBookings;

public static class ViewingBookingDependencyInjection
{
    public static IServiceCollection AddInfrastructureViewingBookings(this IServiceCollection services)
    {
        services.AddScoped<IPublicViewingSlotQueryService, PublicViewingSlotQueryService>();
        services.AddScoped<ICustomerViewingBookingService, CustomerViewingBookingService>();
        return services;
    }
}
