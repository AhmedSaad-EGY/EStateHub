using EstateHub.Application.Customers;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.Customers;

public static class CustomerDependencyInjection
{
    public static IServiceCollection AddInfrastructureCustomers(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICustomerSelfService, CustomerSelfService>();

        return services;
    }
}
