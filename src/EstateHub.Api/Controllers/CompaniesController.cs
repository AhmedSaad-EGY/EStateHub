using EstateHub.Api.Contracts.Companies;
using EstateHub.Application.Companies;
using EstateHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/companies")]
public sealed class CompaniesController(
    IPublicCompanyQueryService publicCompanyQueryService) : ControllerBase
{
    private const int MaximumPageNumber = 10000;
    private const int MaximumPageSize = 50;
    private const int MaximumSearchLength = 100;
    private const int MaximumSlugLength = 200;

    [HttpGet]
    public async Task<ActionResult<CompanyDirectoryResponse>> GetCompanies(
        [FromQuery] CompanyDirectoryRequest request,
        CancellationToken cancellationToken)
    {
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

        if (!TryParseCompanyType(request.CompanyType, out var companyType))
        {
            ModelState.AddModelError(
                nameof(request.CompanyType),
                "CompanyType must be Developer or BrokerAgency.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await publicCompanyQueryService.GetCompaniesAsync(
            new CompanyDirectoryQuery(
                request.PageNumber,
                request.PageSize,
                search,
                companyType,
                request.LocationId),
            cancellationToken);

        return Ok(CompanyDirectoryResponse.From(result));
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<CompanyDetailsResponse>> GetCompanyBySlug(
        string? slug,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = slug?.Trim();

        if (string.IsNullOrEmpty(normalizedSlug)
            || normalizedSlug.Length > MaximumSlugLength)
        {
            ModelState.AddModelError(
                nameof(slug),
                $"Slug is required and must not exceed {MaximumSlugLength} characters.");

            return ValidationProblem(ModelState);
        }

        var company = await publicCompanyQueryService.GetCompanyBySlugAsync(
            normalizedSlug,
            cancellationToken);

        return company is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Company not found.")
            : Ok(CompanyDetailsResponse.From(company));
    }

    private static bool TryParseCompanyType(
        string? value,
        out CompanyType? companyType)
    {
        if (value is null)
        {
            companyType = null;
            return true;
        }

        if (string.Equals(
            value,
            "Developer",
            StringComparison.OrdinalIgnoreCase))
        {
            companyType = CompanyType.Developer;
            return true;
        }

        if (string.Equals(
            value,
            "BrokerAgency",
            StringComparison.OrdinalIgnoreCase))
        {
            companyType = CompanyType.BrokerAgency;
            return true;
        }

        companyType = null;
        return false;
    }
}
