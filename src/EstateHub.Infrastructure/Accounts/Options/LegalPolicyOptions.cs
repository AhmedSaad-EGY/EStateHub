namespace EstateHub.Infrastructure.Accounts.Options;

public sealed class LegalPolicyOptions
{
    public const string SectionName = "LegalPolicies";

    public string TermsAndConditionsVersion { get; set; } = string.Empty;
    public string PrivacyPolicyVersion { get; set; } = string.Empty;
}
