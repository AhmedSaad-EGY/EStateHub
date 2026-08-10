namespace EstateHub.Application.CompanyRoles;

public interface ICompanyRoleManagementService
{
    Task<BootstrapCompanyOwnerResult> BootstrapOwnerAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompanyPermissionGroup>?> GetPermissionsAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompanyRoleSummary>?> GetRolesAsync(
        Guid applicationUserId,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<CompanyRoleDetails?> GetRoleAsync(
        Guid applicationUserId,
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task<CompanyRoleMutationResult> CreateRoleAsync(
        Guid applicationUserId,
        CreateCompanyRoleCommand command,
        CancellationToken cancellationToken = default);

    Task<CompanyRoleMutationResult> UpdateRoleAsync(
        Guid applicationUserId,
        Guid roleId,
        UpdateCompanyRoleCommand command,
        CancellationToken cancellationToken = default);

    Task<CompanyRoleMutationResult> ReplaceRolePermissionsAsync(
        Guid applicationUserId,
        Guid roleId,
        ReplaceCompanyRolePermissionsCommand command,
        CancellationToken cancellationToken = default);

    Task<CompanyRoleMutationStatus> DeactivateRoleAsync(
        Guid applicationUserId,
        Guid roleId,
        CancellationToken cancellationToken = default);
}
