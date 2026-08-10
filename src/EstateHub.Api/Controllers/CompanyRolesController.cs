using EstateHub.Api.Authorization.CompanyPermissions;
using EstateHub.Api.Contracts.CompanyRoles;
using EstateHub.Application.CompanyAccess;
using EstateHub.Application.CompanyRoles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/company")]
public sealed class CompanyRolesController(
    ICompanyRoleManagementService companyRoleManagementService) : ControllerBase
{
    private const int MaximumRoleNameLength = 100;

    [HttpGet("permissions")]
    [RequireCompanyPermission(CompanyPermissionCodes.RolesRead)]
    public async Task<ActionResult<IReadOnlyList<CompanyPermissionGroupResponse>>>
        GetPermissions(CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        var groups = await companyRoleManagementService.GetPermissionsAsync(
            applicationUserId,
            cancellationToken);

        return groups is null
            ? CompanyContextNotFound()
            : Ok(groups.Select(CompanyPermissionGroupResponse.From).ToList());
    }

    [HttpGet("roles")]
    [RequireCompanyPermission(CompanyPermissionCodes.RolesRead)]
    public async Task<ActionResult<IReadOnlyList<CompanyRoleSummaryResponse>>> GetRoles(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        var roles = await companyRoleManagementService.GetRolesAsync(
            applicationUserId,
            includeInactive,
            cancellationToken);

        return roles is null
            ? CompanyContextNotFound()
            : Ok(roles.Select(CompanyRoleSummaryResponse.From).ToList());
    }

    [HttpGet("roles/{roleId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.RolesRead)]
    public async Task<ActionResult<CompanyRoleDetailsResponse>> GetRole(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        if (roleId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(roleId), "RoleId is required.");
            return ValidationProblem(ModelState);
        }

        var role = await companyRoleManagementService.GetRoleAsync(
            applicationUserId,
            roleId,
            cancellationToken);

        return role is null
            ? CompanyRoleNotFound()
            : Ok(CompanyRoleDetailsResponse.From(role));
    }

    [HttpPost("roles")]
    [RequireCompanyPermission(CompanyPermissionCodes.RolesManage)]
    public async Task<ActionResult<CompanyRoleDetailsResponse>> CreateRole(
        CreateCompanyRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        if (!TryCreateRoleCommand(request, out var command))
        {
            return ValidationProblem(ModelState);
        }

        var result = await companyRoleManagementService.CreateRoleAsync(
            applicationUserId,
            command,
            cancellationToken);

        return ToRoleMutationResult(result, created: true);
    }

    [HttpPut("roles/{roleId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.RolesManage)]
    public async Task<ActionResult<CompanyRoleDetailsResponse>> UpdateRole(
        Guid roleId,
        UpdateCompanyRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        if (roleId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(roleId), "RoleId is required.");
        }

        if (!TryCreateUpdateRoleCommand(request, out var command)
            || !ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await companyRoleManagementService.UpdateRoleAsync(
            applicationUserId,
            roleId,
            command,
            cancellationToken);

        return ToRoleMutationResult(result, created: false);
    }

    [HttpPut("roles/{roleId}/permissions")]
    [RequireCompanyPermission(CompanyPermissionCodes.RolesManage)]
    public async Task<ActionResult<CompanyRoleDetailsResponse>> ReplacePermissions(
        Guid roleId,
        ReplaceCompanyRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        if (roleId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(roleId), "RoleId is required.");
        }

        if (!TryCreateReplacePermissionsCommand(request, out var command)
            || !ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await companyRoleManagementService.ReplaceRolePermissionsAsync(
            applicationUserId,
            roleId,
            command,
            cancellationToken);

        return ToRoleMutationResult(result, created: false);
    }

    [HttpDelete("roles/{roleId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.RolesManage)]
    public async Task<IActionResult> DeactivateRole(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        if (roleId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(roleId), "RoleId is required.");
            return ValidationProblem(ModelState);
        }

        var status = await companyRoleManagementService.DeactivateRoleAsync(
            applicationUserId,
            roleId,
            cancellationToken);

        return status switch
        {
            CompanyRoleMutationStatus.Succeeded => NoContent(),
            CompanyRoleMutationStatus.NotFound => CompanyRoleNotFound(),
            CompanyRoleMutationStatus.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Company role conflict."),
            _ => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Company role operation is temporarily unavailable.")
        };
    }

    private bool TryCreateRoleCommand(
        CreateCompanyRoleRequest request,
        out CreateCompanyRoleCommand command)
    {
        var name = ValidateAndNormalizeRoleName(request.Name);
        var permissionCodes = ValidatePermissionCodes(request.PermissionCodes);

        if (!ModelState.IsValid)
        {
            command = null!;
            return false;
        }

        command = new CreateCompanyRoleCommand(name!, permissionCodes!);
        return true;
    }

    private bool TryCreateUpdateRoleCommand(
        UpdateCompanyRoleRequest request,
        out UpdateCompanyRoleCommand command)
    {
        var name = ValidateAndNormalizeRoleName(request.Name);

        if (!ModelState.IsValid)
        {
            command = null!;
            return false;
        }

        command = new UpdateCompanyRoleCommand(name!);
        return true;
    }

    private bool TryCreateReplacePermissionsCommand(
        ReplaceCompanyRolePermissionsRequest request,
        out ReplaceCompanyRolePermissionsCommand command)
    {
        var permissionCodes = ValidatePermissionCodes(request.PermissionCodes);

        if (!ModelState.IsValid)
        {
            command = null!;
            return false;
        }

        command = new ReplaceCompanyRolePermissionsCommand(permissionCodes!);
        return true;
    }

    private string? ValidateAndNormalizeRoleName(string? value)
    {
        var name = value?.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length > MaximumRoleNameLength)
        {
            ModelState.AddModelError(
                "name",
                $"Name is required and must not exceed {MaximumRoleNameLength} characters.");
        }
        else if (string.Equals(name, "Owner", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("name", "Owner is reserved.");
        }

        return name;
    }

    private IReadOnlyList<string>? ValidatePermissionCodes(
        IReadOnlyList<string?>? values)
    {
        if (values is null)
        {
            ModelState.AddModelError("permissionCodes", "PermissionCodes is required.");
            return null;
        }

        if (values.Count > CompanyPermissionCodes.All.Count)
        {
            ModelState.AddModelError(
                "permissionCodes",
                $"PermissionCodes must contain at most {CompanyPermissionCodes.All.Count} items.");
        }

        var normalizedCodes = new List<string>(values.Count);
        var uniqueCodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var value in values)
        {
            var code = value?.Trim();

            if (string.IsNullOrWhiteSpace(code)
                || !CompanyPermissionCodes.IsDefined(code)
                || !uniqueCodes.Add(code))
            {
                ModelState.AddModelError(
                    "permissionCodes",
                    "PermissionCodes must contain unique, active, defined company permission codes.");
                continue;
            }

            normalizedCodes.Add(code);
        }

        return normalizedCodes;
    }

    private ActionResult<CompanyRoleDetailsResponse> ToRoleMutationResult(
        CompanyRoleMutationResult result,
        bool created)
    {
        return result.Status switch
        {
            CompanyRoleMutationStatus.Succeeded when created => CreatedAtAction(
                nameof(GetRole),
                new { roleId = result.Role!.Id },
                CompanyRoleDetailsResponse.From(result.Role)),
            CompanyRoleMutationStatus.Succeeded => Ok(
                CompanyRoleDetailsResponse.From(result.Role!)),
            CompanyRoleMutationStatus.NotFound => CompanyRoleNotFound(),
            CompanyRoleMutationStatus.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Company role conflict."),
            CompanyRoleMutationStatus.InvalidPermissions => ValidationProblem(
                new ValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["permissionCodes"] =
                        ["PermissionCodes must contain active, defined company permission codes."]
                    })),
            _ => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Company role operation is temporarily unavailable.")
        };
    }

    private bool TryGetApplicationUserId(out Guid applicationUserId)
    {
        var subject = User.FindFirst("sub")?.Value;

        return Guid.TryParse(subject, out applicationUserId)
            && applicationUserId != Guid.Empty;
    }

    private ObjectResult CompanyContextNotFound() => Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Company context not found.");

    private ObjectResult CompanyRoleNotFound() => Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Company role not found.");
}
