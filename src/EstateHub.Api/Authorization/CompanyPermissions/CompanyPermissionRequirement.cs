using EstateHub.Application.CompanyAccess;
using Microsoft.AspNetCore.Authorization;

namespace EstateHub.Api.Authorization.CompanyPermissions;

public sealed class CompanyPermissionRequirement : IAuthorizationRequirement
{
    public CompanyPermissionRequirement(string permissionCode)
    {
        if (!CompanyPermissionCodes.IsDefined(permissionCode))
        {
            throw new ArgumentException(
                "A defined company permission code is required.",
                nameof(permissionCode));
        }

        PermissionCode = permissionCode;
    }

    public string PermissionCode { get; }
}
