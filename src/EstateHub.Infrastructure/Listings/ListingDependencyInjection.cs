using EstateHub.Application.Listings;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.Listings;

public static class ListingDependencyInjection
{
    public static IServiceCollection AddInfrastructureListings(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IPublicListingQueryService, PublicListingQueryService>();

        return services;
    }
}
