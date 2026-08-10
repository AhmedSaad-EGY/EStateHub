namespace EstateHub.Api.RateLimiting;

public sealed class AuthRateLimitingOptions
{
    public const string SectionName = "AuthRateLimiting";

    public AuthRateLimitWindowOptions Registration { get; init; } = new();
    public AuthRateLimitWindowOptions Login { get; init; } = new();
    public AuthRateLimitWindowOptions EmailDelivery { get; init; } = new();
    public AuthRateLimitWindowOptions TokenLifecycle { get; init; } = new();
    public AuthRateLimitWindowOptions Verification { get; init; } = new();
}

public sealed class AuthRateLimitWindowOptions
{
    public int PermitLimit { get; init; }
    public int WindowSeconds { get; init; }
}
