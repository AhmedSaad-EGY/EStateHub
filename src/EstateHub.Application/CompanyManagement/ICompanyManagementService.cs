using EstateHub.Application.Common;

namespace EstateHub.Application.CompanyManagement;

public interface ICompanyManagementService
{
    Task<CompanyProfileDetails?> GetProfileAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken = default);

    Task<CompanyProfileMutationResult> UpdateProfileAsync(
        Guid applicationUserId,
        UpdateCompanyProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<PagedResult<CompanyEmployeeDetails>?> GetEmployeesAsync(
        Guid applicationUserId,
        EmployeeDirectoryQuery query,
        CancellationToken cancellationToken = default);

    Task<CompanyEmployeeDetails?> GetEmployeeAsync(
        Guid applicationUserId,
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task<CompanyEmployeeMutationResult> AddEmployeeAsync(
        Guid applicationUserId,
        AddCompanyEmployeeCommand command,
        CancellationToken cancellationToken = default);

    Task<CompanyEmployeeMutationResult> UpdateEmployeeAsync(
        Guid applicationUserId,
        Guid employeeId,
        UpdateCompanyEmployeeCommand command,
        CancellationToken cancellationToken = default);

    Task<CompanyManagementStatus> SuspendEmployeeAsync(
        Guid applicationUserId,
        Guid employeeId,
        EmployeeRowVersionCommand command,
        CancellationToken cancellationToken = default);

    Task<CompanyManagementStatus> ActivateEmployeeAsync(
        Guid applicationUserId,
        Guid employeeId,
        EmployeeRowVersionCommand command,
        CancellationToken cancellationToken = default);

    Task<CompanyManagementStatus> EndEmployeeAsync(
        Guid applicationUserId,
        Guid employeeId,
        EmployeeRowVersionCommand command,
        CancellationToken cancellationToken = default);

    Task<CompanyEmployeeMutationResult> ReplaceEmployeeRolesAsync(
        Guid applicationUserId,
        Guid employeeId,
        ReplaceCompanyEmployeeRolesCommand command,
        CancellationToken cancellationToken = default);

    Task<CompanyManagementStatus> TransferPrimaryContactAsync(
        Guid applicationUserId,
        Guid employeeId,
        TransferPrimaryContactCommand command,
        CancellationToken cancellationToken = default);
}
