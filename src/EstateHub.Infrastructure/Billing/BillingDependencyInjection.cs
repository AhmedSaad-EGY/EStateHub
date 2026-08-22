using EstateHub.Application.Billing;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.Billing;

public static class BillingDependencyInjection
{
    public static IServiceCollection AddInfrastructureBilling(this IServiceCollection services)
    {
        services.AddScoped<ICompanyBillingService, CompanyBillingService>();
        return services;
    }
}
