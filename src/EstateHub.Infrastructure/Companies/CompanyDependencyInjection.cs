using EstateHub.Application.Companies;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.Companies;

public static class CompanyDependencyInjection
{
    public static IServiceCollection AddInfrastructureCompanies(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IPublicCompanyQueryService, PublicCompanyQueryService>();

        return services;
    }
}
