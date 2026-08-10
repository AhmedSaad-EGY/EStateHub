namespace EstateHub.Api.RateLimiting;

public static class AuthRateLimitPolicies
{
    public const string Registration = "auth-registration";
    public const string Login = "auth-login";
    public const string EmailDelivery = "auth-email-delivery";
    public const string TokenLifecycle = "auth-token-lifecycle";
    public const string Verification = "auth-verification";
}
