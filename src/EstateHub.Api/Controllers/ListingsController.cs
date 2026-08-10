using EstateHub.Api.Contracts.Listings;
using EstateHub.Application.Listings;
using EstateHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/listings")]
public sealed class ListingsController(
    IPublicListingQueryService publicListingQueryService) : ControllerBase
{
    private const int MaximumPageNumber = 10000;
    private const int MaximumPageSize = 50;
    private const int MaximumSearchLength = 100;
    private const int MaximumSlugLength = 200;

    [HttpGet]
    public async Task<ActionResult<ListingDirectoryResponse>> GetListings(
        [FromQuery] ListingDirectoryRequest request,
        CancellationToken cancellationToken)
    {
        var search = request.Search?.Trim();

        if (string.IsNullOrEmpty(search))
        {
            search = null;
        }

        ValidatePage(request.PageNumber, request.PageSize);

        if (search is { Length: > MaximumSearchLength })
        {
            ModelState.AddModelError(
                nameof(request.Search),
                $"Search must not exceed {MaximumSearchLength} characters.");
        }

        if (!TryParseListingType(
            request.ListingType?.Trim(),
            out var listingType))
        {
            ModelState.AddModelError(
                nameof(request.ListingType),
                "ListingType must be Sale or Rent.");
        }

        if (!TryNormalizeCurrencyCode(
            request.CurrencyCode,
            out var currencyCode))
        {
            ModelState.AddModelError(
                nameof(request.CurrencyCode),
                "CurrencyCode must contain exactly three ASCII letters.");
        }

        if (!TryParseSort(request.Sort?.Trim(), out var sort))
        {
            ModelState.AddModelError(
                nameof(request.Sort),
                "Sort must be Newest, PriceLowToHigh, PriceHighToLow, AreaLowToHigh, or AreaHighToLow.");
        }

        ValidateRange(
            request.MinPrice,
            request.MaxPrice,
            nameof(request.MinPrice),
            nameof(request.MaxPrice),
            "Price");
        ValidateRange(
            request.MinBedrooms,
            request.MaxBedrooms,
            nameof(request.MinBedrooms),
            nameof(request.MaxBedrooms),
            "Bedrooms");
        ValidateRange(
            request.MinBathrooms,
            request.MaxBathrooms,
            nameof(request.MinBathrooms),
            nameof(request.MaxBathrooms),
            "Bathrooms");
        ValidateRange(
            request.MinArea,
            request.MaxArea,
            nameof(request.MinArea),
            nameof(request.MaxArea),
            "Area");

        if ((request.MinPrice is not null || request.MaxPrice is not null)
            && currencyCode is null)
        {
            ModelState.AddModelError(
                nameof(request.CurrencyCode),
                "CurrencyCode is required when filtering by price.");
        }

        if (sort is ListingDirectorySort.PriceLowToHigh
                or ListingDirectorySort.PriceHighToLow
            && currencyCode is null)
        {
            ModelState.AddModelError(
                nameof(request.CurrencyCode),
                "CurrencyCode is required when sorting by price.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await publicListingQueryService.GetListingsAsync(
            new ListingDirectoryQuery(
                request.PageNumber,
                request.PageSize,
                search,
                listingType,
                request.LocationId,
                request.UnitTypeId,
                request.CompanyId,
                request.ProjectId,
                currencyCode,
                request.MinPrice,
                request.MaxPrice,
                request.MinBedrooms,
                request.MaxBedrooms,
                request.MinBathrooms,
                request.MaxBathrooms,
                request.MinArea,
                request.MaxArea,
                sort),
            cancellationToken);

        return Ok(ListingDirectoryResponse.From(result));
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<ListingDetailsResponse>> GetListingBySlug(
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

        var listing = await publicListingQueryService.GetListingBySlugAsync(
            normalizedSlug,
            cancellationToken);

        return listing is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Listing not found.")
            : Ok(ListingDetailsResponse.From(listing));
    }

    private void ValidatePage(int pageNumber, int pageSize)
    {
        if (pageNumber is < 1 or > MaximumPageNumber)
        {
            ModelState.AddModelError(
                nameof(pageNumber),
                $"PageNumber must be between 1 and {MaximumPageNumber}.");
        }

        if (pageSize is < 1 or > MaximumPageSize)
        {
            ModelState.AddModelError(
                nameof(pageSize),
                $"PageSize must be between 1 and {MaximumPageSize}.");
        }
    }

    private void ValidateRange(
        decimal? minimum,
        decimal? maximum,
        string minimumKey,
        string maximumKey,
        string label)
    {
        if (minimum is < 0)
        {
            ModelState.AddModelError(
                minimumKey,
                $"Minimum {label.ToLowerInvariant()} must be non-negative.");
        }

        if (maximum is < 0)
        {
            ModelState.AddModelError(
                maximumKey,
                $"Maximum {label.ToLowerInvariant()} must be non-negative.");
        }

        if (minimum is not null
            && maximum is not null
            && minimum > maximum)
        {
            ModelState.AddModelError(
                maximumKey,
                $"Minimum {label.ToLowerInvariant()} must not exceed maximum {label.ToLowerInvariant()}.");
        }
    }

    private void ValidateRange(
        int? minimum,
        int? maximum,
        string minimumKey,
        string maximumKey,
        string label)
    {
        if (minimum is < 0)
        {
            ModelState.AddModelError(
                minimumKey,
                $"Minimum {label.ToLowerInvariant()} must be non-negative.");
        }

        if (maximum is < 0)
        {
            ModelState.AddModelError(
                maximumKey,
                $"Maximum {label.ToLowerInvariant()} must be non-negative.");
        }

        if (minimum is not null
            && maximum is not null
            && minimum > maximum)
        {
            ModelState.AddModelError(
                maximumKey,
                $"Minimum {label.ToLowerInvariant()} must not exceed maximum {label.ToLowerInvariant()}.");
        }
    }

    private static bool TryParseListingType(
        string? value,
        out ListingType? listingType)
    {
        if (value is null)
        {
            listingType = null;
            return true;
        }

        if (string.Equals(value, "Sale", StringComparison.OrdinalIgnoreCase))
        {
            listingType = ListingType.Sale;
            return true;
        }

        if (string.Equals(value, "Rent", StringComparison.OrdinalIgnoreCase))
        {
            listingType = ListingType.Rent;
            return true;
        }

        listingType = null;
        return false;
    }

    private static bool TryNormalizeCurrencyCode(
        string? value,
        out string? currencyCode)
    {
        if (value is null)
        {
            currencyCode = null;
            return true;
        }

        var normalizedValue = value.Trim().ToUpperInvariant();

        if (normalizedValue.Length == 3
            && normalizedValue.All(character => character is >= 'A' and <= 'Z'))
        {
            currencyCode = normalizedValue;
            return true;
        }

        currencyCode = null;
        return false;
    }

    private static bool TryParseSort(
        string? value,
        out ListingDirectorySort sort)
    {
        if (value is null
            || string.Equals(value, "Newest", StringComparison.OrdinalIgnoreCase))
        {
            sort = ListingDirectorySort.Newest;
            return true;
        }

        if (string.Equals(
            value,
            "PriceLowToHigh",
            StringComparison.OrdinalIgnoreCase))
        {
            sort = ListingDirectorySort.PriceLowToHigh;
            return true;
        }

        if (string.Equals(
            value,
            "PriceHighToLow",
            StringComparison.OrdinalIgnoreCase))
        {
            sort = ListingDirectorySort.PriceHighToLow;
            return true;
        }

        if (string.Equals(
            value,
            "AreaLowToHigh",
            StringComparison.OrdinalIgnoreCase))
        {
            sort = ListingDirectorySort.AreaLowToHigh;
            return true;
        }

        if (string.Equals(
            value,
            "AreaHighToLow",
            StringComparison.OrdinalIgnoreCase))
        {
            sort = ListingDirectorySort.AreaHighToLow;
            return true;
        }

        sort = default;
        return false;
    }
}
