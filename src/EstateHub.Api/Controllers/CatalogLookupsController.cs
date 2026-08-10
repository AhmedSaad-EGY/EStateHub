using EstateHub.Api.Contracts.CatalogLookups;
using EstateHub.Application.CatalogLookups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/catalog")]
public sealed class CatalogLookupsController(
    IPublicCatalogLookupService publicCatalogLookupService) : ControllerBase
{
    [HttpGet("locations")]
    public async Task<ActionResult<IReadOnlyList<LocationLookupResponse>>> GetLocations(
        [FromQuery] Guid? parentId,
        CancellationToken cancellationToken)
    {
        var locations = await publicCatalogLookupService.GetLocationsAsync(
            parentId,
            cancellationToken);

        return Ok(locations.Select(LocationLookupResponse.From).ToArray());
    }

    [HttpGet("unit-types")]
    public async Task<ActionResult<IReadOnlyList<UnitTypeLookupResponse>>> GetUnitTypes(
        CancellationToken cancellationToken)
    {
        var unitTypes = await publicCatalogLookupService.GetUnitTypesAsync(
            cancellationToken);

        return Ok(unitTypes.Select(UnitTypeLookupResponse.From).ToArray());
    }

    [HttpGet("currencies")]
    public async Task<ActionResult<IReadOnlyList<CurrencyLookupResponse>>> GetCurrencies(
        CancellationToken cancellationToken)
    {
        var currencies = await publicCatalogLookupService.GetCurrenciesAsync(
            cancellationToken);

        return Ok(currencies.Select(CurrencyLookupResponse.From).ToArray());
    }

    [HttpGet("amenities")]
    public async Task<ActionResult<IReadOnlyList<AmenityLookupResponse>>> GetAmenities(
        [FromQuery] string? appliesTo,
        CancellationToken cancellationToken)
    {
        if (!TryParseAmenityTarget(appliesTo, out var target))
        {
            ModelState.AddModelError(
                nameof(appliesTo),
                "AppliesTo must be Project or Unit.");

            return ValidationProblem(ModelState);
        }

        var amenities = await publicCatalogLookupService.GetAmenitiesAsync(
            target,
            cancellationToken);

        return Ok(amenities.Select(AmenityLookupResponse.From).ToArray());
    }

    private static bool TryParseAmenityTarget(
        string? value,
        out CatalogAmenityTarget? target)
    {
        if (value is null)
        {
            target = null;
            return true;
        }

        if (string.Equals(value, "Project", StringComparison.OrdinalIgnoreCase))
        {
            target = CatalogAmenityTarget.Project;
            return true;
        }

        if (string.Equals(value, "Unit", StringComparison.OrdinalIgnoreCase))
        {
            target = CatalogAmenityTarget.Unit;
            return true;
        }

        target = null;
        return false;
    }
}
