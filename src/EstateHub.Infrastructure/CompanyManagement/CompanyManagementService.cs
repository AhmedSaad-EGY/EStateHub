using System.Data;
using System.Data.Common;
using EstateHub.Application.Common;
using EstateHub.Application.CompanyAccess;
using EstateHub.Application.CompanyManagement;
using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Identity;
using EstateHub.Infrastructure.Identity.Enums;
using EstateHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.CompanyManagement;

public sealed class CompanyManagementService(
    EstateHubDbContext dbContext,
    ICompanyAccessService companyAccessService,
    UserManager<ApplicationUser> userManager,
    TimeProvider timeProvider) : ICompanyManagementService
{
    private const string OwnerNormalizedName = "OWNER";

    public async Task<CompanyProfileDetails?> GetProfileAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(
            applicationUserId,
            CompanyPermissionCodes.CompanyProfileRead,
            cancellationToken);

        return scope is null
            ? null
            : await GetProfileQuery(scope.CompanyId)
                .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CompanyProfileMutationResult> UpdateProfileAsync(
        Guid applicationUserId,
        UpdateCompanyProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(
            applicationUserId,
            CompanyPermissionCodes.CompanyProfileManage,
            cancellationToken);

        if (scope is null)
        {
            return new CompanyProfileMutationResult(CompanyManagementStatus.NotFound);
        }

        var currencyExists = await dbContext.Set<Currency>()
            .AsNoTracking()
            .AnyAsync(currency => currency.Code == command.BaseCurrencyCode,
                cancellationToken);
        var locationExists = await dbContext.Set<Location>()
            .AsNoTracking()
            .AnyAsync(location =>
                location.Id == command.AddressLocationId
                && location.IsActive,
                cancellationToken);

        if (!currencyExists || !locationExists)
        {
            return new CompanyProfileMutationResult(
                CompanyManagementStatus.InvalidReference);
        }

        try
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(cancellationToken);
            var company = await dbContext.Set<Company>()
                .SingleOrDefaultAsync(candidate => candidate.Id == scope.CompanyId,
                    cancellationToken);

            if (company is null)
            {
                return new CompanyProfileMutationResult(
                    CompanyManagementStatus.NotFound);
            }

            var address = await dbContext.Set<Address>()
                .SingleOrDefaultAsync(candidate => candidate.Id == company.AddressId,
                    cancellationToken);

            if (address is null)
            {
                return new CompanyProfileMutationResult(
                    CompanyManagementStatus.NotFound);
            }

            dbContext.Entry(company).Property(candidate => candidate.RowVersion)
                .OriginalValue = command.RowVersion;
            company.DisplayName = command.DisplayName;
            company.BusinessEmail = command.BusinessEmail;
            company.SupportPhone = command.SupportPhone;
            company.Website = command.Website;
            company.TimeZoneId = command.TimeZoneId;
            company.BaseCurrencyCode = command.BaseCurrencyCode;
            company.UpdatedAt = timeProvider.GetUtcNow();
            address.LocationId = command.AddressLocationId;
            address.AddressLine1 = command.AddressLine1;
            address.AddressLine2 = command.AddressLine2;
            address.PostalCode = command.PostalCode;
            address.Latitude = command.Latitude;
            address.Longitude = command.Longitude;

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var profile = await GetProfileQuery(scope.CompanyId)
                .SingleAsync(cancellationToken);
            return new CompanyProfileMutationResult(
                CompanyManagementStatus.Succeeded,
                profile);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new CompanyProfileMutationResult(CompanyManagementStatus.Conflict);
        }
        catch (DbUpdateException)
        {
            return new CompanyProfileMutationResult(
                CompanyManagementStatus.ServiceUnavailable);
        }
        catch (DbException)
        {
            return new CompanyProfileMutationResult(
                CompanyManagementStatus.ServiceUnavailable);
        }
    }

    public async Task<PagedResult<CompanyEmployeeDetails>?> GetEmployeesAsync(
        Guid applicationUserId,
        EmployeeDirectoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(
            applicationUserId,
            CompanyPermissionCodes.EmployeesRead,
            cancellationToken);

        if (scope is null)
        {
            return null;
        }

        var employees = GetEmployeeDetailsQuery(scope.CompanyId);

        if (!query.IncludeEnded)
        {
            employees = employees.Where(employee => employee.EndedAt == null);
        }

        if (query.Status is not null)
        {
            employees = employees.Where(employee => employee.Status == query.Status);
        }

        if (query.Search is not null)
        {
            var search = query.Search;
            employees = employees.Where(employee =>
                employee.FullName.Contains(search)
                || (employee.JobTitle != null && employee.JobTitle.Contains(search))
                || employee.Email.Contains(search));
        }

        var totalCount = await employees.CountAsync(cancellationToken);
        var items = await employees
            .OrderByDescending(employee => employee.IsPrimaryContact)
            .ThenBy(employee => employee.FullName)
            .ThenBy(employee => employee.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CompanyEmployeeDetails>(
            items,
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    public async Task<CompanyEmployeeDetails?> GetEmployeeAsync(
        Guid applicationUserId,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(
            applicationUserId,
            CompanyPermissionCodes.EmployeesRead,
            cancellationToken);

        return scope is null
            ? null
            : await GetEmployeeDetailsQuery(scope.CompanyId)
                .SingleOrDefaultAsync(employee => employee.Id == employeeId,
                    cancellationToken);
    }

    public async Task<CompanyEmployeeMutationResult> AddEmployeeAsync(
        Guid applicationUserId,
        AddCompanyEmployeeCommand command,
        CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(
            applicationUserId,
            CompanyPermissionCodes.EmployeesManage,
            cancellationToken);

        if (scope is null)
        {
            return EmployeeNotFound();
        }

        var user = await userManager.FindByEmailAsync(command.Email);

        if (user is null
            || !user.EmailConfirmed
            || user.AccountStatus != ApplicationUserAccountStatus.Active)
        {
            return EmployeeConflict();
        }

        var hasOpenMembership = await dbContext.Set<CompanyEmployee>()
            .AsNoTracking()
            .AnyAsync(employee =>
                employee.ApplicationUserId == user.Id
                && employee.EndedAt == null,
                cancellationToken);

        if (hasOpenMembership)
        {
            return EmployeeConflict();
        }

        var employee = new CompanyEmployee
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = user.Id,
            CompanyId = scope.CompanyId,
            FullName = command.FullName,
            JobTitle = command.JobTitle,
            IsPrimaryContact = false,
            Status = CompanyEmployeeStatus.Active,
            JoinedAt = timeProvider.GetUtcNow()
        };

        dbContext.Add(employee);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return EmployeeConflict();
        }

        var details = await GetEmployeeDetailsQuery(scope.CompanyId)
            .SingleAsync(candidate => candidate.Id == employee.Id, cancellationToken);
        return EmployeeSucceeded(details);
    }

    public async Task<CompanyEmployeeMutationResult> UpdateEmployeeAsync(
        Guid applicationUserId,
        Guid employeeId,
        UpdateCompanyEmployeeCommand command,
        CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(
            applicationUserId,
            CompanyPermissionCodes.EmployeesManage,
            cancellationToken);

        if (scope is null)
        {
            return EmployeeNotFound();
        }

        var employee = await GetMutableEmployeeAsync(
            scope.CompanyId,
            employeeId,
            cancellationToken);

        if (employee is null)
        {
            return EmployeeNotFound();
        }

        dbContext.Entry(employee).Property(candidate => candidate.RowVersion)
            .OriginalValue = command.RowVersion;
        employee.FullName = command.FullName;
        employee.JobTitle = command.JobTitle;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return EmployeeConflict();
        }

        var details = await GetEmployeeDetailsQuery(scope.CompanyId)
            .SingleAsync(candidate => candidate.Id == employeeId, cancellationToken);
        return EmployeeSucceeded(details);
    }

    public Task<CompanyManagementStatus> SuspendEmployeeAsync(
        Guid applicationUserId,
        Guid employeeId,
        EmployeeRowVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        return ChangeEmployeeStatusAsync(
            applicationUserId,
            employeeId,
            command.RowVersion,
            CompanyEmployeeStatus.Active,
            CompanyEmployeeStatus.Suspended,
            cancellationToken);
    }

    public Task<CompanyManagementStatus> ActivateEmployeeAsync(
        Guid applicationUserId,
        Guid employeeId,
        EmployeeRowVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        return ChangeEmployeeStatusAsync(
            applicationUserId,
            employeeId,
            command.RowVersion,
            CompanyEmployeeStatus.Suspended,
            CompanyEmployeeStatus.Active,
            cancellationToken);
    }

    public async Task<CompanyManagementStatus> EndEmployeeAsync(
        Guid applicationUserId,
        Guid employeeId,
        EmployeeRowVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(
            applicationUserId,
            CompanyPermissionCodes.EmployeesManage,
            cancellationToken);

        if (scope is null)
        {
            return CompanyManagementStatus.NotFound;
        }

        try
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(cancellationToken);
            var employee = await dbContext.Set<CompanyEmployee>()
                .SingleOrDefaultAsync(candidate =>
                    candidate.Id == employeeId
                    && candidate.CompanyId == scope.CompanyId
                    && candidate.EndedAt == null
                    && (candidate.Status == CompanyEmployeeStatus.Active
                        || candidate.Status == CompanyEmployeeStatus.Suspended),
                    cancellationToken);

            if (employee is null)
            {
                return CompanyManagementStatus.NotFound;
            }

            if (employee.IsPrimaryContact || employee.Id == scope.CompanyEmployeeId)
            {
                return CompanyManagementStatus.Conflict;
            }

            dbContext.Entry(employee).Property(candidate => candidate.RowVersion)
                .OriginalValue = command.RowVersion;
            var now = timeProvider.GetUtcNow();
            employee.Status = CompanyEmployeeStatus.Ended;
            employee.EndedAt = now;
            var assignments = await dbContext.Set<CompanyEmployeeRole>()
                .Where(assignment =>
                    assignment.CompanyId == scope.CompanyId
                    && assignment.CompanyEmployeeId == employee.Id
                    && assignment.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var assignment in assignments)
            {
                assignment.RevokedAt = now;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CompanyManagementStatus.Succeeded;
        }
        catch (DbUpdateConcurrencyException)
        {
            return CompanyManagementStatus.Conflict;
        }
        catch (DbUpdateException)
        {
            return CompanyManagementStatus.ServiceUnavailable;
        }
    }

    public async Task<CompanyEmployeeMutationResult> ReplaceEmployeeRolesAsync(
        Guid applicationUserId,
        Guid employeeId,
        ReplaceCompanyEmployeeRolesCommand command,
        CancellationToken cancellationToken = default)
    {
        var scope = await GetDualScopeAsync(applicationUserId, cancellationToken);

        if (scope is null || !AreRoleIdsValid(command.RoleIds))
        {
            return new CompanyEmployeeMutationResult(
                CompanyManagementStatus.InvalidReference);
        }

        try
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(cancellationToken);
            var employee = await dbContext.Set<CompanyEmployee>()
                .SingleOrDefaultAsync(candidate =>
                    candidate.Id == employeeId
                    && candidate.CompanyId == scope.CompanyId
                    && candidate.Status == CompanyEmployeeStatus.Active
                    && candidate.EndedAt == null,
                    cancellationToken);

            if (employee is null)
            {
                return new CompanyEmployeeMutationResult(
                    CompanyManagementStatus.InvalidReference);
            }

            var roles = await dbContext.Set<CompanyRole>()
                .AsNoTracking()
                .Where(role =>
                    role.CompanyId == scope.CompanyId
                    && role.IsActive
                    && command.RoleIds.Contains(role.Id))
                .Select(role => new RoleState(role.Id, role.NormalizedName, role.IsBuiltIn))
                .ToListAsync(cancellationToken);

            if (roles.Count != command.RoleIds.Count)
            {
                return new CompanyEmployeeMutationResult(
                    CompanyManagementStatus.InvalidReference);
            }

            var ownerRoleId = await dbContext.Set<CompanyRole>()
                .AsNoTracking()
                .Where(role =>
                    role.CompanyId == scope.CompanyId
                    && role.IsActive
                    && role.IsBuiltIn
                    && role.NormalizedName == OwnerNormalizedName)
                .Select(role => (Guid?)role.Id)
                .SingleOrDefaultAsync(cancellationToken);
            var requestedRoleIds = command.RoleIds.ToHashSet();

            if (employee.IsPrimaryContact
                && (ownerRoleId is null || !requestedRoleIds.Contains(ownerRoleId.Value)))
            {
                return EmployeeConflict();
            }

            var activeAssignments = await dbContext.Set<CompanyEmployeeRole>()
                .Where(assignment =>
                    assignment.CompanyId == scope.CompanyId
                    && assignment.CompanyEmployeeId == employee.Id
                    && assignment.RevokedAt == null)
                .ToListAsync(cancellationToken);
            var activeRoleIds = activeAssignments
                .Select(assignment => assignment.CompanyRoleId)
                .ToHashSet();

            if (ownerRoleId is null)
            {
                return EmployeeConflict();
            }

            if (!requestedRoleIds.Contains(ownerRoleId.Value))
            {
                var anotherOwnerExists = await dbContext.Set<CompanyEmployeeRole>()
                    .AsNoTracking()
                    .AnyAsync(assignment =>
                        assignment.CompanyId == scope.CompanyId
                        && assignment.CompanyRoleId == ownerRoleId.Value
                        && assignment.CompanyEmployeeId != employee.Id
                        && assignment.RevokedAt == null
                        && assignment.CompanyEmployee.Status == CompanyEmployeeStatus.Active
                        && assignment.CompanyEmployee.EndedAt == null,
                        cancellationToken);

                if (!anotherOwnerExists)
                {
                    return EmployeeConflict();
                }
            }

            var now = timeProvider.GetUtcNow();

            foreach (var assignment in activeAssignments.Where(assignment =>
                         !requestedRoleIds.Contains(assignment.CompanyRoleId)))
            {
                assignment.RevokedAt = now;
            }

            dbContext.AddRange(roles
                .Where(role => !activeRoleIds.Contains(role.Id))
                .Select(role => new CompanyEmployeeRole
                {
                    Id = Guid.NewGuid(),
                    CompanyId = scope.CompanyId,
                    CompanyEmployeeId = employee.Id,
                    CompanyRoleId = role.Id,
                    AssignedAt = now,
                    AssignedByApplicationUserId = applicationUserId
                }));

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var details = await GetEmployeeDetailsQuery(scope.CompanyId)
                .SingleAsync(candidate => candidate.Id == employee.Id, cancellationToken);
            return EmployeeSucceeded(details);
        }
        catch (DbUpdateException)
        {
            return new CompanyEmployeeMutationResult(
                CompanyManagementStatus.ServiceUnavailable);
        }
    }

    public async Task<CompanyManagementStatus> TransferPrimaryContactAsync(
        Guid applicationUserId,
        Guid employeeId,
        TransferPrimaryContactCommand command,
        CancellationToken cancellationToken = default)
    {
        var scope = await GetDualScopeAsync(applicationUserId, cancellationToken);

        if (scope is null)
        {
            return CompanyManagementStatus.NotFound;
        }

        try
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var target = await dbContext.Set<CompanyEmployee>()
                .SingleOrDefaultAsync(employee =>
                    employee.Id == employeeId
                    && employee.CompanyId == scope.CompanyId
                    && employee.Status == CompanyEmployeeStatus.Active
                    && employee.EndedAt == null,
                    cancellationToken);

            if (target is null)
            {
                return CompanyManagementStatus.NotFound;
            }

            if (target.IsPrimaryContact)
            {
                return CompanyManagementStatus.Succeeded;
            }

            var current = await dbContext.Set<CompanyEmployee>()
                .SingleOrDefaultAsync(employee =>
                    employee.CompanyId == scope.CompanyId
                    && employee.IsPrimaryContact
                    && employee.Status == CompanyEmployeeStatus.Active
                    && employee.EndedAt == null,
                    cancellationToken);

            if (current is null)
            {
                return CompanyManagementStatus.Conflict;
            }

            var ownerRoleId = await dbContext.Set<CompanyRole>()
                .AsNoTracking()
                .Where(role =>
                    role.CompanyId == scope.CompanyId
                    && role.IsActive
                    && role.IsBuiltIn
                    && role.NormalizedName == OwnerNormalizedName)
                .Select(role => (Guid?)role.Id)
                .SingleOrDefaultAsync(cancellationToken);

            if (ownerRoleId is null)
            {
                return CompanyManagementStatus.Conflict;
            }

            dbContext.Entry(target).Property(employee => employee.RowVersion)
                .OriginalValue = command.TargetEmployeeRowVersion;
            var now = timeProvider.GetUtcNow();
            current.IsPrimaryContact = false;
            await dbContext.SaveChangesAsync(cancellationToken);

            var hasOwnerAssignment = await dbContext.Set<CompanyEmployeeRole>()
                .AsNoTracking()
                .AnyAsync(assignment =>
                    assignment.CompanyId == scope.CompanyId
                    && assignment.CompanyEmployeeId == target.Id
                    && assignment.CompanyRoleId == ownerRoleId.Value
                    && assignment.RevokedAt == null,
                    cancellationToken);

            if (!hasOwnerAssignment)
            {
                dbContext.Add(new CompanyEmployeeRole
                {
                    Id = Guid.NewGuid(),
                    CompanyId = scope.CompanyId,
                    CompanyEmployeeId = target.Id,
                    CompanyRoleId = ownerRoleId.Value,
                    AssignedAt = now,
                    AssignedByApplicationUserId = applicationUserId
                });
            }

            target.IsPrimaryContact = true;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CompanyManagementStatus.Succeeded;
        }
        catch (DbUpdateConcurrencyException)
        {
            return CompanyManagementStatus.Conflict;
        }
        catch (DbUpdateException)
        {
            return CompanyManagementStatus.Conflict;
        }
    }

    private async Task<CompanyManagementStatus> ChangeEmployeeStatusAsync(
        Guid applicationUserId,
        Guid employeeId,
        byte[] rowVersion,
        CompanyEmployeeStatus expectedStatus,
        CompanyEmployeeStatus nextStatus,
        CancellationToken cancellationToken)
    {
        var scope = await GetScopeAsync(
            applicationUserId,
            CompanyPermissionCodes.EmployeesManage,
            cancellationToken);

        if (scope is null)
        {
            return CompanyManagementStatus.NotFound;
        }

        var employee = await dbContext.Set<CompanyEmployee>()
            .SingleOrDefaultAsync(candidate =>
                candidate.Id == employeeId
                && candidate.CompanyId == scope.CompanyId
                && candidate.EndedAt == null,
                cancellationToken);

        if (employee is null)
        {
            return CompanyManagementStatus.NotFound;
        }

        if (employee.Status != expectedStatus
            || employee.IsPrimaryContact
            || employee.Id == scope.CompanyEmployeeId)
        {
            return CompanyManagementStatus.Conflict;
        }

        dbContext.Entry(employee).Property(candidate => candidate.RowVersion)
            .OriginalValue = rowVersion;
        employee.Status = nextStatus;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return CompanyManagementStatus.Succeeded;
        }
        catch (DbUpdateConcurrencyException)
        {
            return CompanyManagementStatus.Conflict;
        }
        catch (DbUpdateException)
        {
            return CompanyManagementStatus.ServiceUnavailable;
        }
    }

    private Task<CompanyAccessScope?> GetScopeAsync(
        Guid applicationUserId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        return companyAccessService.GetAuthorizedScopeAsync(
            applicationUserId,
            permissionCode,
            cancellationToken);
    }

    private async Task<CompanyAccessScope?> GetDualScopeAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken)
    {
        var employeeScope = await GetScopeAsync(
            applicationUserId,
            CompanyPermissionCodes.EmployeesManage,
            cancellationToken);
        var roleScope = await GetScopeAsync(
            applicationUserId,
            CompanyPermissionCodes.RolesManage,
            cancellationToken);

        return employeeScope is not null
            && roleScope is not null
            && employeeScope.CompanyId == roleScope.CompanyId
            && employeeScope.CompanyEmployeeId == roleScope.CompanyEmployeeId
                ? employeeScope
                : null;
    }

    private IQueryable<CompanyProfileDetails> GetProfileQuery(Guid companyId)
    {
        return dbContext.Set<Company>()
            .AsNoTracking()
            .Where(company => company.Id == companyId)
            .Select(company => new CompanyProfileDetails(
                company.Id,
                company.Slug,
                company.LegalName,
                company.DisplayName,
                company.RegistrationNumber,
                company.TaxId,
                company.CompanyType,
                company.BusinessEmail,
                company.SupportPhone,
                company.Website,
                company.LogoFileAssetId,
                company.CoverFileAssetId,
                company.Status,
                company.VerifiedAt,
                company.TimeZoneId,
                company.BaseCurrencyCode,
                company.CreatedAt,
                company.UpdatedAt,
                company.RowVersion,
                new CompanyAddressDetails(
                    company.Address.Id,
                    new CompanyLocationSummary(
                        company.Address.Location.Id,
                        company.Address.Location.Type,
                        company.Address.Location.NameEn,
                        company.Address.Location.NameAr,
                        company.Address.Location.Slug),
                    company.Address.AddressLine1,
                    company.Address.AddressLine2,
                    company.Address.PostalCode,
                    company.Address.Latitude,
                    company.Address.Longitude)));
    }

    private IQueryable<CompanyEmployeeDetails> GetEmployeeDetailsQuery(Guid companyId)
    {
        return dbContext.Set<CompanyEmployee>()
            .AsNoTracking()
            .Where(employee => employee.CompanyId == companyId)
            .Join(
                dbContext.Users.AsNoTracking(),
                employee => employee.ApplicationUserId,
                user => user.Id,
                (employee, user) => new CompanyEmployeeDetails(
                    employee.Id,
                    user.Email ?? string.Empty,
                    employee.FullName,
                    employee.JobTitle,
                    employee.IsPrimaryContact,
                    employee.Status,
                    employee.JoinedAt,
                    employee.EndedAt,
                    employee.RowVersion,
                    employee.RoleAssignments
                        .Where(assignment =>
                            assignment.RevokedAt == null
                            && assignment.CompanyRole.IsActive)
                        .OrderBy(assignment => assignment.CompanyRole.Name)
                        .ThenBy(assignment => assignment.CompanyRole.Id)
                        .Select(assignment => new CompanyEmployeeRoleItem(
                            assignment.CompanyRole.Id,
                            assignment.CompanyRole.Name,
                            assignment.CompanyRole.IsBuiltIn))
                        .ToList()));
    }

    private Task<CompanyEmployee?> GetMutableEmployeeAsync(
        Guid companyId,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<CompanyEmployee>()
            .SingleOrDefaultAsync(employee =>
                employee.Id == employeeId
                && employee.CompanyId == companyId
                && employee.EndedAt == null
                && (employee.Status == CompanyEmployeeStatus.Active
                    || employee.Status == CompanyEmployeeStatus.Suspended),
                cancellationToken);
    }

    private static bool AreRoleIdsValid(IReadOnlyList<Guid> roleIds)
    {
        return roleIds.Count <= 100
            && roleIds.All(roleId => roleId != Guid.Empty)
            && roleIds.Distinct().Count() == roleIds.Count;
    }

    private static CompanyEmployeeMutationResult EmployeeSucceeded(
        CompanyEmployeeDetails employee) =>
        new(CompanyManagementStatus.Succeeded, employee);

    private static CompanyEmployeeMutationResult EmployeeNotFound() =>
        new(CompanyManagementStatus.NotFound);

    private static CompanyEmployeeMutationResult EmployeeConflict() =>
        new(CompanyManagementStatus.Conflict);

    private sealed record RoleState(
        Guid Id,
        string NormalizedName,
        bool IsBuiltIn);
}
