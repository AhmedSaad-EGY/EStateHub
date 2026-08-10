using EstateHub.Application.Accounts;
using EstateHub.Infrastructure.Accounts.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EstateHub.Infrastructure.Accounts;

public static class AccountDependencyInjection
{
    private const int MaximumPolicyVersionLength = 50;

    public static IServiceCollection AddInfrastructureAccounts(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<LegalPolicyOptions>()
            .Bind(configuration.GetRequiredSection(LegalPolicyOptions.SectionName))
            .Validate(
                options => IsValidPolicyVersion(options.TermsAndConditionsVersion),
                $"{LegalPolicyOptions.SectionName}:TermsAndConditionsVersion is required and must not exceed {MaximumPolicyVersionLength} characters.")
            .Validate(
                options => IsValidPolicyVersion(options.PrivacyPolicyVersion),
                $"{LegalPolicyOptions.SectionName}:PrivacyPolicyVersion is required and must not exceed {MaximumPolicyVersionLength} characters.")
            .ValidateOnStart();

        services
            .AddOptions<FrontendOptions>()
            .Bind(configuration.GetRequiredSection(FrontendOptions.SectionName))
            .Validate(
                options => IsValidFrontendBaseUrl(options.BaseUrl),
                $"{FrontendOptions.SectionName}:BaseUrl must be an absolute HTTP or HTTPS URL.")
            .ValidateOnStart();

        services.AddScoped<IAccountService, AccountService>();

        return services;
    }

    private static bool IsValidPolicyVersion(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Length <= MaximumPolicyVersionLength;
    }

    private static bool IsValidFrontendBaseUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp
                || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrWhiteSpace(uri.Host);
    }
}
