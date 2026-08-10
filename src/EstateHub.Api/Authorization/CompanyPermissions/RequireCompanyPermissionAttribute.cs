using EstateHub.Application.CompanyAccess;
using Microsoft.AspNetCore.Authorization;

namespace EstateHub.Api.Authorization.CompanyPermissions;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    Inherited = true,
    AllowMultiple = true)]
public sealed class RequireCompanyPermissionAttribute : AuthorizeAttribute
{
    public RequireCompanyPermissionAttribute(string permissionCode)
    {
        if (!CompanyPermissionCodes.IsDefined(permissionCode))
        {
            throw new ArgumentException(
                "A defined company permission code is required.",
                nameof(permissionCode));
        }

        Policy = string.Concat(
            CompanyPermissionPolicyProvider.PolicyPrefix,
            permissionCode);
    }
}
