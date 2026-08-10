using EstateHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "The SQL Server connection string cannot be null, empty, or whitespace.",
                nameof(connectionString));
        }

        services.AddDbContext<EstateHubDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }
}
