using EstateHub.Application.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
namespace EstateHub.Infrastructure.Subscriptions;
public static class SubscriptionDependencyInjection { public static IServiceCollection AddInfrastructureSubscriptions(this IServiceCollection services) { services.AddScoped<ISubscriptionService, SubscriptionService>(); return services; } }
