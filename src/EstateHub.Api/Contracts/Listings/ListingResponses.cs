using EstateHub.Application.Common;
using EstateHub.Application.Listings;
using EstateHub.Domain.Enums;

namespace EstateHub.Api.Contracts.Listings;

public sealed record ListingCurrencyResponse(
    string Code,
    string Name,
    string Symbol,
    int DecimalPlaces)
{
    public static ListingCurrencyResponse From(ListingCurrencySummary currency)
    {
        return new ListingCurrencyResponse(
            currency.Code,
            currency.Name,
            currency.Symbol,
            currency.DecimalPlaces);
    }
}

public sealed record ListingCompanyResponse(
    Guid Id,
    string Slug,
    string DisplayName,
    Guid? LogoFileAssetId)
{
    public static ListingCompanyResponse From(ListingCompanySummary company)
    {
        return new ListingCompanyResponse(
            company.Id,
            company.Slug,
            company.DisplayName,
            company.LogoFileAssetId);
    }
}

public sealed record ListingUnitTypeResponse(
    Guid Id,
    string Code,
    string NameEn,
    string NameAr)
{
    public static ListingUnitTypeResponse From(ListingUnitTypeSummary unitType)
    {
        return new ListingUnitTypeResponse(
            unitType.Id,
            unitType.Code,
            unitType.NameEn,
            unitType.NameAr);
    }
}

public sealed record ListingLocationResponse(
    Guid Id,
    string Type,
    string NameEn,
    string NameAr,
    string Slug)
{
    public static ListingLocationResponse From(ListingLocationSummary location)
    {
        return new ListingLocationResponse(
            location.Id,
            ListingEnumText.From(location.Type),
            location.NameEn,
            location.NameAr,
            location.Slug);
    }
}

public sealed record ListingProjectResponse(
    Guid Id,
    string Slug,
    string Name,
    string DeveloperCompanySlug)
{
    public static ListingProjectResponse From(ListingProjectSummary project)
    {
        return new ListingProjectResponse(
            project.Id,
            project.Slug,
            project.Name,
            project.DeveloperCompanySlug);
    }
}

public sealed record ListingUnitResponse(
    Guid Id,
    int Bedrooms,
    int Bathrooms,
    decimal BuiltUpArea,
    decimal? LandArea,
    string? FinishingType,
    string FurnishedStatus,
    string Status,
    ListingUnitTypeResponse UnitType,
    ListingLocationResponse Location,
    ListingProjectResponse? Project)
{
    public static ListingUnitResponse From(ListingUnitSummary unit)
    {
        return new ListingUnitResponse(
            unit.Id,
            unit.Bedrooms,
            unit.Bathrooms,
            unit.BuiltUpArea,
            unit.LandArea,
            unit.FinishingType is null
                ? null
                : ListingEnumText.From(unit.FinishingType.Value),
            ListingEnumText.From(unit.FurnishedStatus),
            ListingEnumText.From(unit.Status),
            ListingUnitTypeResponse.From(unit.UnitType),
            ListingLocationResponse.From(unit.Location),
            unit.Project is null
                ? null
                : ListingProjectResponse.From(unit.Project));
    }
}

public sealed record ListingDirectoryItemResponse(
    Guid Id,
    string Slug,
    string Title,
    string ListingType,
    decimal AskingPrice,
    string? RentPeriod,
    DateTimeOffset? PublishedAt,
    Guid? CoverFileAssetId,
    bool HasActivePaymentPlan,
    ListingCurrencyResponse Currency,
    ListingCompanyResponse Company,
    ListingUnitResponse Unit)
{
    public static ListingDirectoryItemResponse From(ListingDirectoryItem listing)
    {
        return new ListingDirectoryItemResponse(
            listing.Id,
            listing.Slug,
            listing.Title,
            ListingEnumText.From(listing.ListingType),
            listing.AskingPrice,
            listing.RentPeriod is null
                ? null
                : ListingEnumText.From(listing.RentPeriod.Value),
            listing.PublishedAt,
            listing.CoverFileAssetId,
            listing.HasActivePaymentPlan,
            ListingCurrencyResponse.From(listing.Currency),
            ListingCompanyResponse.From(listing.Company),
            ListingUnitResponse.From(listing.Unit));
    }
}

public sealed record ListingDirectoryResponse(
    IReadOnlyList<ListingDirectoryItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public static ListingDirectoryResponse From(
        PagedResult<ListingDirectoryItem> result)
    {
        return new ListingDirectoryResponse(
            result.Items.Select(ListingDirectoryItemResponse.From).ToArray(),
            result.PageNumber,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            result.HasPreviousPage,
            result.HasNextPage);
    }
}

internal static class ListingEnumText
{
    public static string From(ListingType value)
    {
        return value switch
        {
            ListingType.Sale => "Sale",
            ListingType.Rent => "Rent",
            _ => Unsupported(value)
        };
    }

    public static string From(RentPeriod value)
    {
        return value switch
        {
            RentPeriod.Monthly => "Monthly",
            RentPeriod.Yearly => "Yearly",
            _ => Unsupported(value)
        };
    }

    public static string From(LocationType value)
    {
        return value switch
        {
            LocationType.Country => "Country",
            LocationType.Governorate => "Governorate",
            LocationType.City => "City",
            LocationType.District => "District",
            _ => Unsupported(value)
        };
    }

    public static string From(FinishingType value)
    {
        return value switch
        {
            FinishingType.Unfinished => "Unfinished",
            FinishingType.SemiFinished => "SemiFinished",
            FinishingType.Finished => "Finished",
            _ => Unsupported(value)
        };
    }

    public static string From(FurnishedStatus value)
    {
        return value switch
        {
            FurnishedStatus.Unfurnished => "Unfurnished",
            FurnishedStatus.SemiFurnished => "SemiFurnished",
            FurnishedStatus.Furnished => "Furnished",
            _ => Unsupported(value)
        };
    }

    public static string From(UnitStatus value)
    {
        return value switch
        {
            UnitStatus.Available => "Available",
            UnitStatus.Reserved => "Reserved",
            UnitStatus.Sold => "Sold",
            UnitStatus.Rented => "Rented",
            UnitStatus.Withdrawn => "Withdrawn",
            _ => Unsupported(value)
        };
    }

    private static string Unsupported<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        throw new ArgumentOutOfRangeException(
            nameof(value),
            value,
            $"Unsupported {typeof(TEnum).Name} value.");
    }
}
