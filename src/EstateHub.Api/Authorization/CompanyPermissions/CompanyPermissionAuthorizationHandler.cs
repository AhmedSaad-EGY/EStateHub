using EstateHub.Application.CompanyAccess;
using Microsoft.AspNetCore.Authorization;

namespace EstateHub.Api.Authorization.CompanyPermissions;

public sealed class CompanyPermissionAuthorizationHandler(
    ICompanyAccessService companyAccessService)
    : AuthorizationHandler<CompanyPermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CompanyPermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var subject = context.User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(subject, out var applicationUserId)
            || applicationUserId == Guid.Empty)
        {
            return;
        }

        var cancellationToken = context.Resource is HttpContext httpContext
            ? httpContext.RequestAborted
            : CancellationToken.None;
        var scope = await companyAccessService.GetAuthorizedScopeAsync(
            applicationUserId,
            requirement.PermissionCode,
            cancellationToken);

        if (scope is not null)
        {
            context.Succeed(requirement);
        }
    }
}
