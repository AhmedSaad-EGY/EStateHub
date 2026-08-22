using EstateHub.Application.Reviews;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.Reviews;

public static class ReviewDependencyInjection
{
    public static IServiceCollection AddInfrastructureReviews(this IServiceCollection services)
    {
        services.AddScoped<IReviewService, ReviewService>();
        return services;
    }
}
