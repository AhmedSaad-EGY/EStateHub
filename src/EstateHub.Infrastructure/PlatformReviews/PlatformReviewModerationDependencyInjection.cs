using EstateHub.Application.PlatformReviews;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.PlatformReviews;

public static class PlatformReviewModerationDependencyInjection
{
    public static IServiceCollection AddInfrastructurePlatformReviewModeration(
        this IServiceCollection services)
    {
        services.AddScoped<IPlatformReviewModerationService, PlatformReviewModerationService>();
        return services;
    }
}
