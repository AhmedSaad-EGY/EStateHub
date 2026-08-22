using EstateHub.Application.PlatformAccess;
using Microsoft.AspNetCore.Authorization;

namespace EstateHub.Api.Authorization.Platform;

public sealed class PlatformAdminAuthorizationHandler(
    IPlatformAccessService platformAccessService)
    : AuthorizationHandler<PlatformAdminRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PlatformAdminRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true
            || !Guid.TryParse(context.User.FindFirst("sub")?.Value, out var applicationUserId)
            || applicationUserId == Guid.Empty)
        {
            return;
        }

        var cancellationToken = context.Resource is HttpContext httpContext
            ? httpContext.RequestAborted
            : CancellationToken.None;
        if (await platformAccessService.IsPlatformAdminAsync(
                applicationUserId,
                cancellationToken))
        {
            context.Succeed(requirement);
        }
    }
}

