using EstateHub.Application.Promotions;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.Promotions;

public static class PromotionDependencyInjection
{
    public static IServiceCollection AddInfrastructurePromotions(this IServiceCollection services)
    {
        services.AddScoped<IPromotionService, PromotionService>();
        return services;
    }
}
