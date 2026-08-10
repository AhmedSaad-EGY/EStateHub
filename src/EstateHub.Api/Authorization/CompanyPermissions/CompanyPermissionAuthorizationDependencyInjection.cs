using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EstateHub.Api.Authorization.CompanyPermissions;

public static class CompanyPermissionAuthorizationDependencyInjection
{
    public static IServiceCollection AddCompanyPermissionAuthorization(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Replace(
            ServiceDescriptor.Singleton<IAuthorizationPolicyProvider,
                CompanyPermissionPolicyProvider>());
        services.AddScoped<IAuthorizationHandler,
            CompanyPermissionAuthorizationHandler>();

        return services;
    }
}
