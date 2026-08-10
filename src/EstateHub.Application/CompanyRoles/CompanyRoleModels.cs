namespace EstateHub.Application.CompanyRoles;

public sealed record CompanyPermissionItem(
    Guid Id,
    string Code,
    string Name,
    string? Description);

public sealed record CompanyPermissionGroup(
    Guid Id,
    string Name,
    string? Description,
    int SortOrder,
    IReadOnlyList<CompanyPermissionItem> Permissions);

public sealed record CompanyRoleSummary(
    Guid Id,
    string Name,
    bool IsBuiltIn,
    bool IsActive,
    IReadOnlyList<string> ActivePermissionCodes,
    int ActiveEmployeeAssignmentCount);

public sealed record CompanyRoleDetails(
    Guid Id,
    string Name,
    bool IsBuiltIn,
    bool IsActive,
    IReadOnlyList<string> ActivePermissionCodes,
    int ActiveEmployeeAssignmentCount,
    IReadOnlyList<CompanyPermissionItem> ActivePermissions);

public sealed record CreateCompanyRoleCommand(
    string Name,
    IReadOnlyList<string> PermissionCodes);

public sealed record UpdateCompanyRoleCommand(string Name);

public sealed record ReplaceCompanyRolePermissionsCommand(
    IReadOnlyList<string> PermissionCodes);

public enum BootstrapCompanyOwnerStatus
{
    Succeeded,
    AlreadyInitialized,
    NotFound,
    Conflict,
    ServiceUnavailable
}

public sealed record BootstrapCompanyOwnerResult(BootstrapCompanyOwnerStatus Status);

public enum CompanyRoleMutationStatus
{
    Succeeded,
    NotFound,
    Conflict,
    InvalidPermissions,
    ServiceUnavailable
}

public sealed record CompanyRoleMutationResult(
    CompanyRoleMutationStatus Status,
    CompanyRoleDetails? Role = null);
