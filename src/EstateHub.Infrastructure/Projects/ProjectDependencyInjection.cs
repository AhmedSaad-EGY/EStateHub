using EstateHub.Application.Projects;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.Projects;

public static class ProjectDependencyInjection
{
    public static IServiceCollection AddInfrastructureProjects(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IPublicProjectQueryService, PublicProjectQueryService>();

        return services;
    }
}
