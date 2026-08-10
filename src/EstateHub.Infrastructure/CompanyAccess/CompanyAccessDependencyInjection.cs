using EstateHub.Application.CompanyAccess;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.CompanyAccess;

public static class CompanyAccessDependencyInjection
{
    public static IServiceCollection AddInfrastructureCompanyAccess(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICompanyAccessService, CompanyAccessService>();

        return services;
    }
}
