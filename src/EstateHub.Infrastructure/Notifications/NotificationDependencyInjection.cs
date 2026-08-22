using EstateHub.Application.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.Notifications;

public static class NotificationDependencyInjection
{
    public static IServiceCollection AddInfrastructureNotifications(
        this IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationWriter, NotificationWriter>();
        return services;
    }
}
