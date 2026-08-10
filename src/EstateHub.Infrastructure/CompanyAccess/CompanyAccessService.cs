using EstateHub.Application.CompanyAccess;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.CompanyAccess;

public sealed class CompanyAccessService(
    EstateHubDbContext dbContext) : ICompanyAccessService
{
    public async Task<CompanyAccessContext?> GetCurrentContextAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken = default)
    {
        var header = await GetEligibleMemberships()
            .Where(employee => employee.ApplicationUserId == applicationUserId)
            .Select(employee => new CompanyAccessHeader(
                new CompanyAccessCompany(
                    employee.Company.Id,
                    employee.Company.Slug,
                    employee.Company.DisplayName,
                    employee.Company.CompanyType,
                    employee.Company.LogoFileAssetId,
                    employee.Company.TimeZoneId,
                    employee.Company.BaseCurrencyCode,
                    employee.Company.VerifiedAt!.Value),
                new CompanyAccessEmployee(
                    employee.Id,
                    employee.FullName,
                    employee.JobTitle,
                    employee.IsPrimaryContact,
                    employee.JoinedAt)))
            .SingleOrDefaultAsync(cancellationToken);

        if (header is null)
        {
            return null;
        }

        var roles = await dbContext.Set<CompanyEmployeeRole>()
            .AsNoTracking()
            .Where(assignment =>
                assignment.CompanyEmployeeId == header.Employee.Id
                && assignment.CompanyId == header.Company.Id
                && assignment.RevokedAt == null
                && assignment.CompanyRole.IsActive)
            .OrderBy(assignment => assignment.CompanyRole.Name)
            .ThenBy(assignment => assignment.CompanyRole.Id)
            .Select(assignment => new CompanyAccessRole(
                assignment.CompanyRole.Id,
                assignment.CompanyRole.Name,
                assignment.CompanyRole.IsBuiltIn))
            .ToListAsync(cancellationToken);

        var permissionCodes = await dbContext.Set<CompanyEmployeeRole>()
            .AsNoTracking()
            .Where(assignment =>
                assignment.CompanyEmployeeId == header.Employee.Id
                && assignment.CompanyId == header.Company.Id
                && assignment.RevokedAt == null
                && assignment.CompanyRole.IsActive)
            .SelectMany(assignment => assignment.CompanyRole.PermissionAssignments
                .Where(permissionAssignment =>
                    permissionAssignment.RevokedAt == null
                    && permissionAssignment.Permission.IsActive)
                .Select(permissionAssignment => permissionAssignment.Permission.Code))
            .Distinct()
            .OrderBy(code => code)
            .ToListAsync(cancellationToken);

        return new CompanyAccessContext(
            header.Company,
            header.Employee,
            roles,
            permissionCodes);
    }

    public Task<CompanyAccessScope?> GetAuthorizedScopeAsync(
        Guid applicationUserId,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        return GetEligibleMemberships()
            .Where(employee =>
                employee.ApplicationUserId == applicationUserId
                && employee.RoleAssignments.Any(assignment =>
                    assignment.CompanyId == employee.CompanyId
                    && assignment.RevokedAt == null
                    && assignment.CompanyRole.IsActive
                    && assignment.CompanyRole.PermissionAssignments.Any(
                        permissionAssignment =>
                            permissionAssignment.RevokedAt == null
                            && permissionAssignment.Permission.IsActive
                            && permissionAssignment.Permission.Code == permissionCode)))
            .Select(employee => new CompanyAccessScope(
                employee.CompanyId,
                employee.Id))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private IQueryable<CompanyEmployee> GetEligibleMemberships()
    {
        return dbContext.Set<CompanyEmployee>()
            .AsNoTracking()
            .Where(employee =>
                employee.Status == CompanyEmployeeStatus.Active
                && employee.EndedAt == null
                && employee.Company.Status == CompanyStatus.Active
                && employee.Company.VerifiedAt != null);
    }

    private sealed record CompanyAccessHeader(
        CompanyAccessCompany Company,
        CompanyAccessEmployee Employee);
}
