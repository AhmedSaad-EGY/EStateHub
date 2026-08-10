using System.ComponentModel.DataAnnotations;
using EstateHub.Api.Authorization.CompanyPermissions;
using EstateHub.Api.Contracts.CompanyManagement;
using EstateHub.Application.CompanyAccess;
using EstateHub.Application.CompanyManagement;
using EstateHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/company/employees")]
public sealed class CompanyEmployeesController(
    ICompanyManagementService companyManagementService) : ControllerBase
{
    private const int MaximumPageNumber = 10000;
    private const int MaximumPageSize = 50;
    private static readonly EmailAddressAttribute EmailValidator = new();

    [HttpGet]
    [RequireCompanyPermission(CompanyPermissionCodes.EmployeesRead)]
    public async Task<ActionResult<CompanyEmployeesResponse>> GetEmployees(
        [FromQuery] EmployeeDirectoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        if (!TryCreateDirectoryQuery(request, out var query))
        {
            return ValidationProblem(ModelState);
        }

        var employees = await companyManagementService.GetEmployeesAsync(
            applicationUserId,
            query,
            cancellationToken);

        return employees is null
            ? CompanyEmployeesNotFound()
            : Ok(CompanyEmployeesResponse.From(employees));
    }

    [HttpGet("{employeeId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.EmployeesRead)]
    public async Task<ActionResult<CompanyEmployeeResponse>> GetEmployee(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        if (!ValidateEmployeeId(employeeId))
        {
            return ValidationProblem(ModelState);
        }

        var employee = await companyManagementService.GetEmployeeAsync(
            applicationUserId,
            employeeId,
            cancellationToken);

        return employee is null
            ? CompanyEmployeeNotFound()
            : Ok(CompanyEmployeeResponse.From(employee));
    }

    [HttpPost]
    [RequireCompanyPermission(CompanyPermissionCodes.EmployeesManage)]
    public async Task<ActionResult<CompanyEmployeeResponse>> AddEmployee(
        AddCompanyEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        if (!TryCreateAddCommand(request, out var command))
        {
            return ValidationProblem(ModelState);
        }

        var result = await companyManagementService.AddEmployeeAsync(
            applicationUserId,
            command,
            cancellationToken);

        return result.Status switch
        {
            CompanyManagementStatus.Succeeded => CreatedAtAction(
                nameof(GetEmployee),
                new { employeeId = result.Employee!.Id },
                CompanyEmployeeResponse.From(result.Employee)),
            CompanyManagementStatus.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Employee could not be added."),
            CompanyManagementStatus.NotFound => CompanyEmployeesNotFound(),
            _ => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Employee operation is temporarily unavailable.")
        };
    }

    [HttpPut("{employeeId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.EmployeesManage)]
    public async Task<ActionResult<CompanyEmployeeResponse>> UpdateEmployee(
        Guid employeeId,
        UpdateCompanyEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        ValidateEmployeeId(employeeId);

        if (!TryCreateUpdateCommand(request, out var command) || !ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await companyManagementService.UpdateEmployeeAsync(
            applicationUserId,
            employeeId,
            command,
            cancellationToken);

        return ToEmployeeMutationResult(result);
    }

    [HttpPost("{employeeId}/suspend")]
    [RequireCompanyPermission(CompanyPermissionCodes.EmployeesManage)]
    public Task<IActionResult> SuspendEmployee(
        Guid employeeId,
        EmployeeRowVersionRequest request,
        CancellationToken cancellationToken) =>
        ChangeEmployeeStatusAsync(employeeId, request, true, cancellationToken);

    [HttpPost("{employeeId}/activate")]
    [RequireCompanyPermission(CompanyPermissionCodes.EmployeesManage)]
    public Task<IActionResult> ActivateEmployee(
        Guid employeeId,
        EmployeeRowVersionRequest request,
        CancellationToken cancellationToken) =>
        ChangeEmployeeStatusAsync(employeeId, request, false, cancellationToken);

    [HttpPost("{employeeId}/end")]
    [RequireCompanyPermission(CompanyPermissionCodes.EmployeesManage)]
    public async Task<IActionResult> EndEmployee(
        Guid employeeId,
        EmployeeRowVersionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        ValidateEmployeeId(employeeId);
        var rowVersion = ParseRowVersion(request.RowVersion, "rowVersion");

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var status = await companyManagementService.EndEmployeeAsync(
            applicationUserId,
            employeeId,
            new EmployeeRowVersionCommand(rowVersion!),
            cancellationToken);

        return ToStatusResult(status);
    }

    [HttpPut("{employeeId}/roles")]
    [RequireCompanyPermission(CompanyPermissionCodes.EmployeesManage)]
    [RequireCompanyPermission(CompanyPermissionCodes.RolesManage)]
    public async Task<ActionResult<CompanyEmployeeResponse>> ReplaceRoles(
        Guid employeeId,
        ReplaceCompanyEmployeeRolesRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        ValidateEmployeeId(employeeId);
        var roleIds = ValidateRoleIds(request.RoleIds);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await companyManagementService.ReplaceEmployeeRolesAsync(
            applicationUserId,
            employeeId,
            new ReplaceCompanyEmployeeRolesCommand(roleIds!),
            cancellationToken);

        return result.Status switch
        {
            CompanyManagementStatus.Succeeded => Ok(
                CompanyEmployeeResponse.From(result.Employee!)),
            CompanyManagementStatus.InvalidReference => ValidationProblem(
                new ValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["roleIds"] = ["The employee or one or more role IDs are unavailable."]
                    })),
            _ => ToEmployeeMutationResult(result)
        };
    }

    [HttpPut("{employeeId}/primary-contact")]
    [RequireCompanyPermission(CompanyPermissionCodes.EmployeesManage)]
    [RequireCompanyPermission(CompanyPermissionCodes.RolesManage)]
    public async Task<IActionResult> TransferPrimaryContact(
        Guid employeeId,
        TransferPrimaryContactRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        ValidateEmployeeId(employeeId);
        var rowVersion = ParseRowVersion(
            request.TargetEmployeeRowVersion,
            "targetEmployeeRowVersion");

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var status = await companyManagementService.TransferPrimaryContactAsync(
            applicationUserId,
            employeeId,
            new TransferPrimaryContactCommand(rowVersion!),
            cancellationToken);

        return ToStatusResult(status);
    }

    private async Task<IActionResult> ChangeEmployeeStatusAsync(
        Guid employeeId,
        EmployeeRowVersionRequest request,
        bool suspend,
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        ValidateEmployeeId(employeeId);
        var rowVersion = ParseRowVersion(request.RowVersion, "rowVersion");

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var command = new EmployeeRowVersionCommand(rowVersion!);
        var status = suspend
            ? await companyManagementService.SuspendEmployeeAsync(
                applicationUserId, employeeId, command, cancellationToken)
            : await companyManagementService.ActivateEmployeeAsync(
                applicationUserId, employeeId, command, cancellationToken);

        return ToStatusResult(status);
    }

    private bool TryCreateDirectoryQuery(
        EmployeeDirectoryRequest request,
        out EmployeeDirectoryQuery query)
    {
        var search = request.Search?.Trim();

        if (request.PageNumber is < 1 or > MaximumPageNumber)
        {
            ModelState.AddModelError(
                "pageNumber",
                $"PageNumber must be between 1 and {MaximumPageNumber}.");
        }

        if (request.PageSize is < 1 or > MaximumPageSize)
        {
            ModelState.AddModelError(
                "pageSize",
                $"PageSize must be between 1 and {MaximumPageSize}.");
        }

        if (request.Search is not null
            && (string.IsNullOrWhiteSpace(search) || search.Length > 100))
        {
            ModelState.AddModelError(
                "search",
                "Search must be non-blank and at most 100 characters when supplied.");
        }

        if (!TryParseStatus(request.Status, out var status))
        {
            ModelState.AddModelError(
                "status",
                "Status must be Active, Suspended, or Ended.");
        }

        if (!ModelState.IsValid)
        {
            query = null!;
            return false;
        }

        query = new EmployeeDirectoryQuery(
            request.PageNumber,
            request.PageSize,
            search,
            status,
            request.IncludeEnded);
        return true;
    }

    private bool TryCreateAddCommand(
        AddCompanyEmployeeRequest request,
        out AddCompanyEmployeeCommand command)
    {
        var email = request.Email?.Trim();
        var fullName = ValidateRequired(request.FullName, "fullName", 200);
        var jobTitle = ValidateOptional(request.JobTitle, "jobTitle", 150);

        if (string.IsNullOrWhiteSpace(email)
            || email.Length > 256
            || !EmailValidator.IsValid(email))
        {
            ModelState.AddModelError("email", "Email must be valid and at most 256 characters.");
        }

        if (!ModelState.IsValid)
        {
            command = null!;
            return false;
        }

        command = new AddCompanyEmployeeCommand(email!, fullName!, jobTitle);
        return true;
    }

    private bool TryCreateUpdateCommand(
        UpdateCompanyEmployeeRequest request,
        out UpdateCompanyEmployeeCommand command)
    {
        var fullName = ValidateRequired(request.FullName, "fullName", 200);
        var jobTitle = ValidateOptional(request.JobTitle, "jobTitle", 150);
        var rowVersion = ParseRowVersion(request.RowVersion, "rowVersion");

        if (!ModelState.IsValid)
        {
            command = null!;
            return false;
        }

        command = new UpdateCompanyEmployeeCommand(fullName!, jobTitle, rowVersion!);
        return true;
    }

    private string? ValidateRequired(string? value, string key, int maximumLength)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maximumLength)
        {
            ModelState.AddModelError(
                key,
                $"{key} is required and must not exceed {maximumLength} characters.");
        }

        return normalized;
    }

    private string? ValidateOptional(string? value, string key, int maximumLength)
    {
        var normalized = value?.Trim();

        if (normalized is { Length: > 0 } && normalized.Length > maximumLength)
        {
            ModelState.AddModelError(key, $"{key} must not exceed {maximumLength} characters.");
        }

        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private IReadOnlyList<Guid>? ValidateRoleIds(IReadOnlyList<Guid>? roleIds)
    {
        if (roleIds is null
            || roleIds.Count > 100
            || roleIds.Any(roleId => roleId == Guid.Empty)
            || roleIds.Distinct().Count() != roleIds.Count)
        {
            ModelState.AddModelError(
                "roleIds",
                "RoleIds must contain at most 100 unique non-empty IDs.");
            return null;
        }

        return roleIds;
    }

    private byte[]? ParseRowVersion(string? value, string key)
    {
        try
        {
            var bytes = string.IsNullOrWhiteSpace(value)
                ? null
                : Convert.FromBase64String(value);

            if (bytes is { Length: 8 })
            {
                return bytes;
            }
        }
        catch (FormatException)
        {
            // The validation error below is intentionally generic.
        }

        ModelState.AddModelError(key, "RowVersion must be Base64 encoded eight bytes.");
        return null;
    }

    private bool ValidateEmployeeId(Guid employeeId)
    {
        if (employeeId != Guid.Empty)
        {
            return true;
        }

        ModelState.AddModelError("employeeId", "EmployeeId is required.");
        return false;
    }

    private static bool TryParseStatus(
        string? value,
        out CompanyEmployeeStatus? status)
    {
        if (value is null)
        {
            status = null;
            return true;
        }

        var normalized = value.Trim();

        if (string.Equals(normalized, "Active", StringComparison.Ordinal))
        {
            status = CompanyEmployeeStatus.Active;
            return true;
        }

        if (string.Equals(normalized, "Suspended", StringComparison.Ordinal))
        {
            status = CompanyEmployeeStatus.Suspended;
            return true;
        }

        if (string.Equals(normalized, "Ended", StringComparison.Ordinal))
        {
            status = CompanyEmployeeStatus.Ended;
            return true;
        }

        status = null;
        return false;
    }

    private ActionResult<CompanyEmployeeResponse> ToEmployeeMutationResult(
        CompanyEmployeeMutationResult result)
    {
        return result.Status switch
        {
            CompanyManagementStatus.Succeeded => Ok(
                CompanyEmployeeResponse.From(result.Employee!)),
            CompanyManagementStatus.NotFound => CompanyEmployeeNotFound(),
            CompanyManagementStatus.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Employee operation conflict."),
            CompanyManagementStatus.InvalidReference => ValidationProblem(
                new ValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["employee"] = ["The employee is unavailable."]
                    })),
            _ => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Employee operation is temporarily unavailable.")
        };
    }

    private IActionResult ToStatusResult(CompanyManagementStatus status)
    {
        return status switch
        {
            CompanyManagementStatus.Succeeded => NoContent(),
            CompanyManagementStatus.NotFound => CompanyEmployeeNotFound(),
            CompanyManagementStatus.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Employee operation conflict."),
            _ => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Employee operation is temporarily unavailable.")
        };
    }

    private bool TryGetApplicationUserId(out Guid applicationUserId)
    {
        var subject = User.FindFirst("sub")?.Value;

        return Guid.TryParse(subject, out applicationUserId)
            && applicationUserId != Guid.Empty;
    }

    private ObjectResult CompanyEmployeesNotFound() => Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Company employees not found.");

    private ObjectResult CompanyEmployeeNotFound() => Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Company employee not found.");
}
