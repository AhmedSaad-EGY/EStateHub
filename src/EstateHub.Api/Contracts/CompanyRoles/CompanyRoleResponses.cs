using EstateHub.Application.CompanyRoles;

namespace EstateHub.Api.Contracts.CompanyRoles;

public sealed record CompanyPermissionResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description)
{
    public static CompanyPermissionResponse From(CompanyPermissionItem permission) =>
        new(
            permission.Id,
            permission.Code,
            permission.Name,
            permission.Description);
}

public sealed record CompanyPermissionGroupResponse(
    Guid Id,
    string Name,
    string? Description,
    int SortOrder,
    IReadOnlyList<CompanyPermissionResponse> Permissions)
{
    public static CompanyPermissionGroupResponse From(
        CompanyPermissionGroup group) =>
        new(
            group.Id,
            group.Name,
            group.Description,
            group.SortOrder,
            group.Permissions.Select(CompanyPermissionResponse.From).ToList());
}

public sealed record CompanyRoleSummaryResponse(
    Guid Id,
    string Name,
    bool IsBuiltIn,
    bool IsActive,
    IReadOnlyList<string> ActivePermissionCodes,
    int ActiveEmployeeAssignmentCount)
{
    public static CompanyRoleSummaryResponse From(CompanyRoleSummary role) =>
        new(
            role.Id,
            role.Name,
            role.IsBuiltIn,
            role.IsActive,
            role.ActivePermissionCodes,
            role.ActiveEmployeeAssignmentCount);
}

public sealed record CompanyRoleDetailsResponse(
    Guid Id,
    string Name,
    bool IsBuiltIn,
    bool IsActive,
    IReadOnlyList<string> ActivePermissionCodes,
    int ActiveEmployeeAssignmentCount,
    IReadOnlyList<CompanyPermissionResponse> ActivePermissions)
{
    public static CompanyRoleDetailsResponse From(CompanyRoleDetails role) =>
        new(
            role.Id,
            role.Name,
            role.IsBuiltIn,
            role.IsActive,
            role.ActivePermissionCodes,
            role.ActiveEmployeeAssignmentCount,
            role.ActivePermissions.Select(CompanyPermissionResponse.From).ToList());
}
