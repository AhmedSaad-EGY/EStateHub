using EstateHub.Api.Contracts.Projects;
using EstateHub.Application.Projects;
using EstateHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/companies/{companySlug}/projects")]
public sealed class CompanyProjectsController(
    IPublicProjectQueryService publicProjectQueryService) : ControllerBase
{
    private const int MaximumPageNumber = 10000;
    private const int MaximumPageSize = 50;
    private const int MaximumSearchLength = 100;
    private const int MaximumSlugLength = 200;

    [HttpGet]
    public async Task<ActionResult<ProjectDirectoryResponse>> GetProjects(
        string? companySlug,
        [FromQuery] ProjectDirectoryRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedCompanySlug = NormalizeSlug(
            companySlug,
            nameof(companySlug));
        var search = request.Search?.Trim();

        if (string.IsNullOrEmpty(search))
        {
            search = null;
        }

        if (request.PageNumber is < 1 or > MaximumPageNumber)
        {
            ModelState.AddModelError(
                nameof(request.PageNumber),
                $"PageNumber must be between 1 and {MaximumPageNumber}.");
        }

        if (request.PageSize is < 1 or > MaximumPageSize)
        {
            ModelState.AddModelError(
                nameof(request.PageSize),
                $"PageSize must be between 1 and {MaximumPageSize}.");
        }

        if (search is { Length: > MaximumSearchLength })
        {
            ModelState.AddModelError(
                nameof(request.Search),
                $"Search must not exceed {MaximumSearchLength} characters.");
        }

        if (!TryParseDeliveryStatus(
            request.DeliveryStatus?.Trim(),
            out var deliveryStatus))
        {
            ModelState.AddModelError(
                nameof(request.DeliveryStatus),
                "DeliveryStatus must be Planned, UnderConstruction, ReadyToMove, or Delivered.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await publicProjectQueryService.GetProjectsAsync(
            normalizedCompanySlug!,
            new ProjectDirectoryQuery(
                request.PageNumber,
                request.PageSize,
                search,
                deliveryStatus,
                request.LocationId),
            cancellationToken);

        return result is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Company not found.")
            : Ok(ProjectDirectoryResponse.From(result));
    }

    [HttpGet("{projectSlug}")]
    public async Task<ActionResult<ProjectDetailsResponse>> GetProjectBySlug(
        string? companySlug,
        string? projectSlug,
        CancellationToken cancellationToken)
    {
        var normalizedCompanySlug = NormalizeSlug(
            companySlug,
            nameof(companySlug));
        var normalizedProjectSlug = NormalizeSlug(
            projectSlug,
            nameof(projectSlug));

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var project = await publicProjectQueryService.GetProjectBySlugAsync(
            normalizedCompanySlug!,
            normalizedProjectSlug!,
            cancellationToken);

        return project is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Project not found.")
            : Ok(ProjectDetailsResponse.From(project));
    }

    private string? NormalizeSlug(string? value, string key)
    {
        var normalizedValue = value?.Trim();

        if (string.IsNullOrEmpty(normalizedValue)
            || normalizedValue.Length > MaximumSlugLength)
        {
            ModelState.AddModelError(
                key,
                $"{key} is required and must not exceed {MaximumSlugLength} characters.");
        }

        return normalizedValue;
    }

    private static bool TryParseDeliveryStatus(
        string? value,
        out ProjectDeliveryStatus? deliveryStatus)
    {
        if (value is null)
        {
            deliveryStatus = null;
            return true;
        }

        if (string.Equals(
            value,
            "Planned",
            StringComparison.OrdinalIgnoreCase))
        {
            deliveryStatus = ProjectDeliveryStatus.Planned;
            return true;
        }

        if (string.Equals(
            value,
            "UnderConstruction",
            StringComparison.OrdinalIgnoreCase))
        {
            deliveryStatus = ProjectDeliveryStatus.UnderConstruction;
            return true;
        }

        if (string.Equals(
            value,
            "ReadyToMove",
            StringComparison.OrdinalIgnoreCase))
        {
            deliveryStatus = ProjectDeliveryStatus.ReadyToMove;
            return true;
        }

        if (string.Equals(
            value,
            "Delivered",
            StringComparison.OrdinalIgnoreCase))
        {
            deliveryStatus = ProjectDeliveryStatus.Delivered;
            return true;
        }

        deliveryStatus = null;
        return false;
    }
}
