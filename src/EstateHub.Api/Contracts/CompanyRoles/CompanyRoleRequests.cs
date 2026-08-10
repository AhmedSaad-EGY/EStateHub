namespace EstateHub.Api.Contracts.CompanyRoles;

public sealed record CreateCompanyRoleRequest(
    string? Name,
    IReadOnlyList<string?>? PermissionCodes);

public sealed record UpdateCompanyRoleRequest(string? Name);

public sealed record ReplaceCompanyRolePermissionsRequest(
    IReadOnlyList<string?>? PermissionCodes);
