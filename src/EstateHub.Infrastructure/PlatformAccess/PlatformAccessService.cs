using EstateHub.Application.PlatformAccess;
using EstateHub.Infrastructure.Identity;
using EstateHub.Infrastructure.Identity.Configurations;
using EstateHub.Infrastructure.Identity.Enums;
using EstateHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.PlatformAccess;

public sealed class PlatformAccessService(
    EstateHubDbContext dbContext) : IPlatformAccessService
{
    public Task<bool> IsPlatformAdminAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<ApplicationUser>()
            .AsNoTracking()
            .AnyAsync(user =>
                user.Id == applicationUserId
                && user.AccountStatus == ApplicationUserAccountStatus.Active
                && user.EmailConfirmed
                && dbContext.Set<IdentityUserRole<Guid>>().Any(assignment =>
                    assignment.UserId == user.Id
                    && dbContext.Set<IdentityRole<Guid>>().Any(role =>
                        role.Id == assignment.RoleId
                        && role.Id == PlatformRoleConfiguration.PlatformAdminRoleId
                        && role.NormalizedName == PlatformRoleNames.NormalizedPlatformAdmin)),
                cancellationToken);
}
