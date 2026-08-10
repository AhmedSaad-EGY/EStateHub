using EstateHub.Api.Contracts.CompanyAccess;
using EstateHub.Application.CompanyAccess;
using EstateHub.Application.CompanyRoles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/me/company-context")]
public sealed class CompanyContextController(
    ICompanyAccessService companyAccessService,
    ICompanyRoleManagementService companyRoleManagementService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CompanyAccessContextResponse>> GetCurrentContext(
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        var context = await companyAccessService.GetCurrentContextAsync(
            applicationUserId,
            cancellationToken);

        return context is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Company context not found.")
            : Ok(CompanyAccessContextResponse.From(context));
    }

    [HttpPost("bootstrap-owner")]
    public async Task<IActionResult> BootstrapOwner(
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        var result = await companyRoleManagementService.BootstrapOwnerAsync(
            applicationUserId,
            cancellationToken);

        return result.Status switch
        {
            BootstrapCompanyOwnerStatus.Succeeded
                or BootstrapCompanyOwnerStatus.AlreadyInitialized => NoContent(),
            BootstrapCompanyOwnerStatus.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Company context not found."),
            BootstrapCompanyOwnerStatus.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Company owner bootstrap conflict."),
            _ => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Company owner bootstrap is temporarily unavailable.")
        };
    }

    private bool TryGetApplicationUserId(out Guid applicationUserId)
    {
        var subject = User.FindFirst("sub")?.Value;

        return Guid.TryParse(subject, out applicationUserId)
            && applicationUserId != Guid.Empty;
    }
}
