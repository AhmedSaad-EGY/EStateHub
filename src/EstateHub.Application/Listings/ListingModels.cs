using EstateHub.Domain.Enums;

namespace EstateHub.Application.Listings;

public enum ListingDirectorySort
{
    Newest,
    PriceLowToHigh,
    PriceHighToLow,
    AreaLowToHigh,
    AreaHighToLow
}

public sealed record ListingDirectoryQuery(
    int PageNumber,
    int PageSize,
    string? Search,
    ListingType? ListingType,
    Guid? LocationId,
    Guid? UnitTypeId,
    Guid? CompanyId,
    Guid? ProjectId,
    string? CurrencyCode,
    decimal? MinPrice,
    decimal? MaxPrice,
    int? MinBedrooms,
    int? MaxBedrooms,
    int? MinBathrooms,
    int? MaxBathrooms,
    decimal? MinArea,
    decimal? MaxArea,
    ListingDirectorySort Sort);

public sealed record ListingLocationSummary(
    Guid Id,
    LocationType Type,
    string NameEn,
    string NameAr,
    string Slug);

public sealed record ListingCurrencySummary(
    string Code,
    string Name,
    string Symbol,
    int DecimalPlaces);

public sealed record ListingCompanySummary(
    Guid Id,
    string Slug,
    string DisplayName,
    Guid? LogoFileAssetId);

public sealed record ListingUnitTypeSummary(
    Guid Id,
    string Code,
    string NameEn,
    string NameAr);

public sealed record ListingProjectSummary(
    Guid Id,
    string Slug,
    string Name,
    string DeveloperCompanySlug);

public sealed record ListingUnitSummary(
    Guid Id,
    int Bedrooms,
    int Bathrooms,
    decimal BuiltUpArea,
    decimal? LandArea,
    FinishingType? FinishingType,
    FurnishedStatus FurnishedStatus,
    UnitStatus Status,
    ListingUnitTypeSummary UnitType,
    ListingLocationSummary Location,
    ListingProjectSummary? Project);

public sealed record ListingDirectoryItem(
    Guid Id,
    string Slug,
    string Title,
    ListingType ListingType,
    decimal AskingPrice,
    RentPeriod? RentPeriod,
    DateTimeOffset? PublishedAt,
    Guid? CoverFileAssetId,
    bool HasActivePaymentPlan,
    ListingCurrencySummary Currency,
    ListingCompanySummary Company,
    ListingUnitSummary Unit);
