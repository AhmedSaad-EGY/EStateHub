using EstateHub.Application.CompanyAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace EstateHub.Api.Authorization.CompanyPermissions;

public sealed class CompanyPermissionPolicyProvider : IAuthorizationPolicyProvider
{
    internal const string PolicyPrefix = "CompanyPermission:";

    private readonly DefaultAuthorizationPolicyProvider _fallbackProvider;

    public CompanyPermissionPolicyProvider(
        IOptions<AuthorizationOptions> options)
    {
        _fallbackProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!string.IsNullOrEmpty(policyName)
            && policyName.StartsWith(PolicyPrefix, StringComparison.Ordinal))
        {
            var permissionCode = policyName[PolicyPrefix.Length..];

            if (CompanyPermissionCodes.IsDefined(permissionCode))
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(
                        new CompanyPermissionRequirement(permissionCode))
                    .Build();

                return Task.FromResult<AuthorizationPolicy?>(policy);
            }
        }

        return _fallbackProvider.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallbackProvider.GetFallbackPolicyAsync();
    }
}
