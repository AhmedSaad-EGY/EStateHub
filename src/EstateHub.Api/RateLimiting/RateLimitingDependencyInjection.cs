using System.Globalization;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace EstateHub.Api.RateLimiting;

public static class RateLimitingDependencyInjection
{
    private const int MaximumPermitLimit = 1000;
    private const int MaximumWindowSeconds = 86400;
    private const string UnknownPartitionKey = "unknown";

    public static IServiceCollection AddAuthRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(AuthRateLimitingOptions.SectionName);

        services
            .AddOptions<AuthRateLimitingOptions>()
            .Bind(section)
            .Validate(
                options => IsValid(options.Registration),
                ValidationMessage(nameof(AuthRateLimitingOptions.Registration)))
            .Validate(
                options => IsValid(options.Login),
                ValidationMessage(nameof(AuthRateLimitingOptions.Login)))
            .Validate(
                options => IsValid(options.EmailDelivery),
                ValidationMessage(nameof(AuthRateLimitingOptions.EmailDelivery)))
            .Validate(
                options => IsValid(options.TokenLifecycle),
                ValidationMessage(nameof(AuthRateLimitingOptions.TokenLifecycle)))
            .Validate(
                options => IsValid(options.Verification),
                ValidationMessage(nameof(AuthRateLimitingOptions.Verification)))
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionResponseAsync;

            AddFixedWindowPolicy(
                options,
                AuthRateLimitPolicies.Registration,
                configured => configured.Registration);
            AddFixedWindowPolicy(
                options,
                AuthRateLimitPolicies.Login,
                configured => configured.Login);
            AddFixedWindowPolicy(
                options,
                AuthRateLimitPolicies.EmailDelivery,
                configured => configured.EmailDelivery);
            AddFixedWindowPolicy(
                options,
                AuthRateLimitPolicies.TokenLifecycle,
                configured => configured.TokenLifecycle);
            AddFixedWindowPolicy(
                options,
                AuthRateLimitPolicies.Verification,
                configured => configured.Verification);
        });

        return services;
    }

    private static void AddFixedWindowPolicy(
        RateLimiterOptions options,
        string policyName,
        Func<AuthRateLimitingOptions, AuthRateLimitWindowOptions> selectOptions)
    {
        options.AddPolicy(
            policyName,
            httpContext =>
            {
                var configuredOptions = httpContext.RequestServices
                    .GetRequiredService<IOptions<AuthRateLimitingOptions>>()
                    .Value;
                var policyOptions = selectOptions(configuredOptions);

                return RateLimitPartition.GetFixedWindowLimiter(
                    GetPartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = policyOptions.PermitLimit,
                        Window = TimeSpan.FromSeconds(policyOptions.WindowSeconds),
                        AutoReplenishment = true,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });
    }

    private static string GetPartitionKey(HttpContext httpContext)
    {
        var remoteIpAddress = httpContext.Connection.RemoteIpAddress;

        return remoteIpAddress is null
            ? UnknownPartitionKey
            : remoteIpAddress.MapToIPv6().ToString();
    }

    private static async ValueTask WriteRejectionResponseAsync(
        OnRejectedContext context,
        CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            var retryAfterSeconds = Math.Max(
                1L,
                (long)Math.Ceiling(retryAfter.TotalSeconds));

            response.Headers.RetryAfter = retryAfterSeconds.ToString(
                CultureInfo.InvariantCulture);
        }

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests.",
            Detail = "Too many requests. Please try again later."
        };

        await response.WriteAsJsonAsync(
            problemDetails,
            (JsonSerializerOptions?)null,
            "application/problem+json",
            cancellationToken);
    }

    private static bool IsValid(AuthRateLimitWindowOptions? options)
    {
        return options is not null
            && options.PermitLimit is > 0 and <= MaximumPermitLimit
            && options.WindowSeconds is > 0 and <= MaximumWindowSeconds;
    }

    private static string ValidationMessage(string policyName)
    {
        return $"{AuthRateLimitingOptions.SectionName}:{policyName} must configure "
            + $"PermitLimit between 1 and {MaximumPermitLimit} and WindowSeconds "
            + $"between 1 and {MaximumWindowSeconds}.";
    }
}
