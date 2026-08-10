using System.Data;
using System.Data.Common;
using EstateHub.Application.CompanyAccess;
using EstateHub.Application.CompanyRoles;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.CompanyRoles;

public sealed class CompanyRoleManagementService(
    EstateHubDbContext dbContext,
    ICompanyAccessService companyAccessService,
    TimeProvider timeProvider) : ICompanyRoleManagementService
{
    private const string OwnerRoleName = "Owner";
    private const string OwnerNormalizedName = "OWNER";

    public async Task<BootstrapCompanyOwnerResult> BootstrapOwnerAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var membership = await dbContext.Set<CompanyEmployee>()
                .Where(employee =>
                    employee.ApplicationUserId == applicationUserId
                    && employee.IsPrimaryContact
                    && employee.Status == CompanyEmployeeStatus.Active
                    && employee.EndedAt == null
                    && employee.Company.Status == CompanyStatus.Active
                    && employee.Company.VerifiedAt != null)
                .SingleOrDefaultAsync(cancellationToken);

            if (membership is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new BootstrapCompanyOwnerResult(
                    BootstrapCompanyOwnerStatus.NotFound);
            }

            var companyId = membership.CompanyId;
            var roles = await dbContext.Set<CompanyRole>()
                .AsNoTracking()
                .Where(role => role.CompanyId == companyId)
                .Select(role => new OwnerRoleState(
                    role.Id,
                    role.Name,
                    role.NormalizedName,
                    role.IsBuiltIn,
                    role.IsActive))
                .ToListAsync(cancellationToken);
            var grants = await dbContext.Set<CompanyRolePermission>()
                .AsNoTracking()
                .Where(grant => grant.CompanyRole.CompanyId == companyId)
                .Select(grant => new OwnerGrantState(
                    grant.CompanyRoleId,
                    grant.Permission.Code,
                    grant.Permission.IsActive,
                    grant.RevokedAt))
                .ToListAsync(cancellationToken);
            var assignments = await dbContext.Set<CompanyEmployeeRole>()
                .AsNoTracking()
                .Where(assignment => assignment.CompanyId == companyId)
                .Select(assignment => new OwnerAssignmentState(
                    assignment.CompanyEmployeeId,
                    assignment.CompanyRoleId,
                    assignment.RevokedAt))
                .ToListAsync(cancellationToken);

            if (roles.Count != 0 || grants.Count != 0 || assignments.Count != 0)
            {
                var isComplete = IsCompleteOwnerState(
                    roles,
                    grants,
                    assignments,
                    membership.Id);
                await transaction.RollbackAsync(cancellationToken);

                return new BootstrapCompanyOwnerResult(isComplete
                    ? BootstrapCompanyOwnerStatus.AlreadyInitialized
                    : BootstrapCompanyOwnerStatus.Conflict);
            }

            var permissions = await GetActivePermissionsByCodesAsync(
                CompanyPermissionCodes.All,
                cancellationToken);

            if (permissions.Count != CompanyPermissionCodes.All.Count)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new BootstrapCompanyOwnerResult(
                    BootstrapCompanyOwnerStatus.ServiceUnavailable);
            }

            var now = timeProvider.GetUtcNow();
            var ownerRole = new CompanyRole
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = OwnerRoleName,
                NormalizedName = OwnerNormalizedName,
                IsBuiltIn = true,
                IsActive = true
            };

            dbContext.Add(ownerRole);
            dbContext.AddRange(permissions.Select(permission => new CompanyRolePermission
            {
                Id = Guid.NewGuid(),
                CompanyRoleId = ownerRole.Id,
                PermissionId = permission.Id,
                GrantedAt = now,
                GrantedByApplicationUserId = applicationUserId
            }));
            dbContext.Add(new CompanyEmployeeRole
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                CompanyEmployeeId = membership.Id,
                CompanyRoleId = ownerRole.Id,
                AssignedAt = now,
                AssignedByApplicationUserId = applicationUserId
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new BootstrapCompanyOwnerResult(
                BootstrapCompanyOwnerStatus.Succeeded);
        }
        catch (DbUpdateException)
        {
            return new BootstrapCompanyOwnerResult(
                BootstrapCompanyOwnerStatus.ServiceUnavailable);
        }
        catch (DbException)
        {
            return new BootstrapCompanyOwnerResult(
                BootstrapCompanyOwnerStatus.ServiceUnavailable);
        }
    }

    public async Task<IReadOnlyList<CompanyPermissionGroup>?> GetPermissionsAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(
            applicationUserId,
            CompanyPermissionCodes.RolesRead,
            cancellationToken);

        if (scope is null)
        {
            return null;
        }

        return await dbContext.Set<PermissionGroup>()
            .AsNoTracking()
            .Where(group => group.Permissions.Any(permission => permission.IsActive))
            .OrderBy(group => group.SortOrder)
            .ThenBy(group => group.Name)
            .ThenBy(group => group.Id)
            .Select(group => new CompanyPermissionGroup(
                group.Id,
                group.Name,
                group.Description,
                group.SortOrder,
                group.Permissions
                    .Where(permission => permission.IsActive)
                    .OrderBy(permission => permission.Code)
                    .Select(permission => new CompanyPermissionItem(
                        permission.Id,
                        permission.Code,
                        permission.Name,
                        permission.Description))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CompanyRoleSummary>?> GetRolesAsync(
        Guid applicationUserId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(
            applicationUserId,
            CompanyPermissionCodes.RolesRead,
            cancellationToken);

        if (scope is null)
        {
            return null;
        }

        var roles = dbContext.Set<CompanyRole>()
            .AsNoTracking()
            .Where(role => role.CompanyId == scope.CompanyId);

        if (!includeInactive)
        {
            roles = roles.Where(role => role.IsActive);
        }

        return await roles
            .OrderBy(role => role.Name)
            .ThenBy(role => role.Id)
            .Select(role => new CompanyRoleSummary(
                role.Id,
                role.Name,
                role.IsBuiltIn,
                role.IsActive,
                role.PermissionAssignments
                    .Where(grant =>
                        grant.RevokedAt == null
                        && grant.Permission.IsActive)
                    .OrderBy(grant => grant.Permission.Code)
                    .Select(grant => grant.Permission.Code)
                    .ToList(),
                role.EmployeeAssignments.Count(assignment =>
                    assignment.RevokedAt == null
                    && assignment.CompanyEmployee.Status == CompanyEmployeeStatus.Active
                    && assignment.CompanyEmployee.EndedAt == null)))
            .ToListAsync(cancellationToken);
    }

    public async Task<CompanyRoleDetails?> GetRoleAsync(
        Guid applicationUserId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(
            applicationUserId,
            CompanyPermissionCodes.RolesRead,
            cancellationToken);

        return scope is null
            ? null
            : await GetRoleDetailsQuery(scope.CompanyId)
                .SingleOrDefaultAsync(role => role.Id == roleId, cancellationToken);
    }

    public async Task<CompanyRoleMutationResult> CreateRoleAsync(
        Guid applicationUserId,
        CreateCompanyRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(
            applicationUserId,
            CompanyPermissionCodes.RolesManage,
            cancellationToken);

        if (scope is null)
        {
            return NotFound();
        }

        if (!HasActiveDefinedPermissions(command.PermissionCodes))
        {
            return InvalidPermissions();
        }

        var permissions = await GetActivePermissionsByCodesAsync(
            command.PermissionCodes,
            cancellationToken);

        if (permissions.Count != command.PermissionCodes.Count)
        {
            return InvalidPermissions();
        }

        var normalizedName = command.Name.ToUpperInvariant();

        if (await dbContext.Set<CompanyRole>()
            .AsNoTracking()
            .AnyAsync(role =>
                role.CompanyId == scope.CompanyId
                && role.NormalizedName == normalizedName,
                cancellationToken))
        {
            return Conflict();
        }

        try
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(cancellationToken);
            var now = timeProvider.GetUtcNow();
            var role = new CompanyRole
            {
                Id = Guid.NewGuid(),
                CompanyId = scope.CompanyId,
                Name = command.Name,
                NormalizedName = normalizedName,
                IsBuiltIn = false,
                IsActive = true
            };

            dbContext.Add(role);
            dbContext.AddRange(permissions.Select(permission => new CompanyRolePermission
            {
                Id = Guid.NewGuid(),
                CompanyRoleId = role.Id,
                PermissionId = permission.Id,
                GrantedAt = now,
                GrantedByApplicationUserId = applicationUserId
            }));
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var details = await GetRoleDetailsQuery(scope.CompanyId)
                .SingleAsync(candidate => candidate.Id == role.Id, cancellationToken);
            return Succeeded(details);
        }
        catch (DbUpdateException)
        {
            return Conflict();
        }
    }

    public async Task<CompanyRoleMutationResult> UpdateRoleAsync(
        Guid applicationUserId,
        Guid roleId,
        UpdateCompanyRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(
            applicationUserId,
            CompanyPermissionCodes.RolesManage,
            cancellationToken);

        if (scope is null)
        {
            return NotFound();
        }

        var role = await dbContext.Set<CompanyRole>()
            .SingleOrDefaultAsync(candidate =>
                candidate.Id == roleId
                && candidate.CompanyId == scope.CompanyId
                && candidate.IsActive,
                cancellationToken);

        if (role is null)
        {
            return NotFound();
        }

        if (role.IsBuiltIn)
        {
            return Conflict();
        }

        var normalizedName = command.Name.ToUpperInvariant();

        if (await dbContext.Set<CompanyRole>()
            .AsNoTracking()
            .AnyAsync(candidate =>
                candidate.CompanyId == scope.CompanyId
                && candidate.Id != roleId
                && candidate.NormalizedName == normalizedName,
                cancellationToken))
        {
            return Conflict();
        }

        try
        {
            role.Name = command.Name;
            role.NormalizedName = normalizedName;
            await dbContext.SaveChangesAsync(cancellationToken);

            var details = await GetRoleDetailsQuery(scope.CompanyId)
                .SingleAsync(candidate => candidate.Id == roleId, cancellationToken);
            return Succeeded(details);
        }
        catch (DbUpdateException)
        {
            return Conflict();
        }
    }

    public async Task<CompanyRoleMutationResult> ReplaceRolePermissionsAsync(
        Guid applicationUserId,
        Guid roleId,
        ReplaceCompanyRolePermissionsCommand command,
        CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(
            applicationUserId,
            CompanyPermissionCodes.RolesManage,
            cancellationToken);

        if (scope is null)
        {
            return NotFound();
        }

        if (!HasActiveDefinedPermissions(command.PermissionCodes))
        {
            return InvalidPermissions();
        }

        var permissions = await GetActivePermissionsByCodesAsync(
            command.PermissionCodes,
            cancellationToken);

        if (permissions.Count != command.PermissionCodes.Count)
        {
            return InvalidPermissions();
        }

        try
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(cancellationToken);
            var role = await dbContext.Set<CompanyRole>()
                .SingleOrDefaultAsync(candidate =>
                    candidate.Id == roleId
                    && candidate.CompanyId == scope.CompanyId
                    && candidate.IsActive,
                    cancellationToken);

            if (role is null)
            {
                return NotFound();
            }

            if (role.IsBuiltIn)
            {
                return Conflict();
            }

            var now = timeProvider.GetUtcNow();
            var requestedPermissionIds = permissions
                .Select(permission => permission.Id)
                .ToHashSet();
            var activeGrants = await dbContext.Set<CompanyRolePermission>()
                .Where(grant =>
                    grant.CompanyRoleId == role.Id
                    && grant.RevokedAt == null)
                .ToListAsync(cancellationToken);
            var activePermissionIds = activeGrants
                .Select(grant => grant.PermissionId)
                .ToHashSet();

            foreach (var grant in activeGrants.Where(grant =>
                         !requestedPermissionIds.Contains(grant.PermissionId)))
            {
                grant.RevokedAt = now;
            }

            dbContext.AddRange(permissions
                .Where(permission => !activePermissionIds.Contains(permission.Id))
                .Select(permission => new CompanyRolePermission
                {
                    Id = Guid.NewGuid(),
                    CompanyRoleId = role.Id,
                    PermissionId = permission.Id,
                    GrantedAt = now,
                    GrantedByApplicationUserId = applicationUserId
                }));
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var details = await GetRoleDetailsQuery(scope.CompanyId)
                .SingleAsync(candidate => candidate.Id == roleId, cancellationToken);
            return Succeeded(details);
        }
        catch (DbUpdateException)
        {
            return ServiceUnavailable();
        }
    }

    public async Task<CompanyRoleMutationStatus> DeactivateRoleAsync(
        Guid applicationUserId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(
            applicationUserId,
            CompanyPermissionCodes.RolesManage,
            cancellationToken);

        if (scope is null)
        {
            return CompanyRoleMutationStatus.NotFound;
        }

        try
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(cancellationToken);
            var role = await dbContext.Set<CompanyRole>()
                .SingleOrDefaultAsync(candidate =>
                    candidate.Id == roleId
                    && candidate.CompanyId == scope.CompanyId
                    && candidate.IsActive,
                    cancellationToken);

            if (role is null)
            {
                return CompanyRoleMutationStatus.NotFound;
            }

            if (role.IsBuiltIn)
            {
                return CompanyRoleMutationStatus.Conflict;
            }

            var now = timeProvider.GetUtcNow();
            var grants = await dbContext.Set<CompanyRolePermission>()
                .Where(grant =>
                    grant.CompanyRoleId == role.Id
                    && grant.RevokedAt == null)
                .ToListAsync(cancellationToken);
            var assignments = await dbContext.Set<CompanyEmployeeRole>()
                .Where(assignment =>
                    assignment.CompanyRoleId == role.Id
                    && assignment.CompanyId == scope.CompanyId
                    && assignment.RevokedAt == null)
                .ToListAsync(cancellationToken);

            role.IsActive = false;

            foreach (var grant in grants)
            {
                grant.RevokedAt = now;
            }

            foreach (var assignment in assignments)
            {
                assignment.RevokedAt = now;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CompanyRoleMutationStatus.Succeeded;
        }
        catch (DbUpdateException)
        {
            return CompanyRoleMutationStatus.ServiceUnavailable;
        }
    }

    private async Task<CompanyAccessScope?> GetScopeAsync(
        Guid applicationUserId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        return await companyAccessService.GetAuthorizedScopeAsync(
            applicationUserId,
            permissionCode,
            cancellationToken);
    }

    private Task<List<Permission>> GetActivePermissionsByCodesAsync(
        IReadOnlyList<string> permissionCodes,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<Permission>()
            .AsNoTracking()
            .Where(permission =>
                permission.IsActive
                && permissionCodes.Contains(permission.Code))
            .OrderBy(permission => permission.Code)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<CompanyRoleDetails> GetRoleDetailsQuery(Guid companyId)
    {
        return dbContext.Set<CompanyRole>()
            .AsNoTracking()
            .Where(role => role.CompanyId == companyId && role.IsActive)
            .Select(role => new CompanyRoleDetails(
                role.Id,
                role.Name,
                role.IsBuiltIn,
                role.IsActive,
                role.PermissionAssignments
                    .Where(grant =>
                        grant.RevokedAt == null
                        && grant.Permission.IsActive)
                    .OrderBy(grant => grant.Permission.Code)
                    .Select(grant => grant.Permission.Code)
                    .ToList(),
                role.EmployeeAssignments.Count(assignment =>
                    assignment.RevokedAt == null
                    && assignment.CompanyEmployee.Status == CompanyEmployeeStatus.Active
                    && assignment.CompanyEmployee.EndedAt == null),
                role.PermissionAssignments
                    .Where(grant =>
                        grant.RevokedAt == null
                        && grant.Permission.IsActive)
                    .OrderBy(grant => grant.Permission.Code)
                    .Select(grant => new CompanyPermissionItem(
                        grant.Permission.Id,
                        grant.Permission.Code,
                        grant.Permission.Name,
                        grant.Permission.Description))
                    .ToList()));
    }

    private static bool HasActiveDefinedPermissions(
        IReadOnlyList<string> permissionCodes)
    {
        return permissionCodes.Count <= CompanyPermissionCodes.All.Count
            && permissionCodes.All(CompanyPermissionCodes.IsDefined)
            && permissionCodes.Distinct(StringComparer.Ordinal).Count()
                == permissionCodes.Count;
    }

    private static bool IsCompleteOwnerState(
        IReadOnlyList<OwnerRoleState> roles,
        IReadOnlyList<OwnerGrantState> grants,
        IReadOnlyList<OwnerAssignmentState> assignments,
        Guid primaryContactEmployeeId)
    {
        if (roles.Count != 1 || grants.Count != CompanyPermissionCodes.All.Count
            || assignments.Count != 1)
        {
            return false;
        }

        var owner = roles[0];

        return owner.Name == OwnerRoleName
            && owner.NormalizedName == OwnerNormalizedName
            && owner.IsBuiltIn
            && owner.IsActive
            && assignments[0].CompanyEmployeeId == primaryContactEmployeeId
            && assignments[0].CompanyRoleId == owner.Id
            && assignments[0].RevokedAt is null
            && grants.All(grant =>
                grant.CompanyRoleId == owner.Id
                && grant.PermissionIsActive
                && grant.RevokedAt is null)
            && grants.Select(grant => grant.PermissionCode)
                .Order(StringComparer.Ordinal)
                .SequenceEqual(CompanyPermissionCodes.All.Order(StringComparer.Ordinal),
                    StringComparer.Ordinal);
    }

    private static CompanyRoleMutationResult Succeeded(CompanyRoleDetails role) =>
        new(CompanyRoleMutationStatus.Succeeded, role);

    private static CompanyRoleMutationResult NotFound() =>
        new(CompanyRoleMutationStatus.NotFound);

    private static CompanyRoleMutationResult Conflict() =>
        new(CompanyRoleMutationStatus.Conflict);

    private static CompanyRoleMutationResult InvalidPermissions() =>
        new(CompanyRoleMutationStatus.InvalidPermissions);

    private static CompanyRoleMutationResult ServiceUnavailable() =>
        new(CompanyRoleMutationStatus.ServiceUnavailable);

    private sealed record OwnerRoleState(
        Guid Id,
        string Name,
        string NormalizedName,
        bool IsBuiltIn,
        bool IsActive);

    private sealed record OwnerGrantState(
        Guid CompanyRoleId,
        string PermissionCode,
        bool PermissionIsActive,
        DateTimeOffset? RevokedAt);

    private sealed record OwnerAssignmentState(
        Guid CompanyEmployeeId,
        Guid CompanyRoleId,
        DateTimeOffset? RevokedAt);
}
