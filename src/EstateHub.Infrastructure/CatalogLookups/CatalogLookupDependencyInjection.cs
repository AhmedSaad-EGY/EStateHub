using EstateHub.Application.CatalogLookups;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.CatalogLookups;

public static class CatalogLookupDependencyInjection
{
    public static IServiceCollection AddInfrastructureCatalogLookups(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IPublicCatalogLookupService, PublicCatalogLookupService>();

        return services;
    }
}
