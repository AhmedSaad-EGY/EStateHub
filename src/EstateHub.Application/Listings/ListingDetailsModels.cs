using EstateHub.Domain.Enums;

namespace EstateHub.Application.Listings;

public sealed record ListingDetailsCompany(
    Guid Id,
    string Slug,
    string DisplayName,
    Guid? LogoFileAssetId,
    string BusinessEmail,
    string SupportPhone,
    string? Website);

public sealed record ListingDetailsProjectDeveloper(
    Guid Id,
    string Slug,
    string DisplayName,
    Guid? LogoFileAssetId);

public sealed record ListingDetailsProject(
    Guid Id,
    string Slug,
    string Name,
    ProjectDeliveryStatus DeliveryStatus,
    DateOnly? ExpectedDeliveryDate,
    ListingDetailsProjectDeveloper DeveloperCompany);

public sealed record ListingDetailsUnit(
    Guid Id,
    int Bedrooms,
    int Bathrooms,
    int? FloorNumber,
    int? TotalFloors,
    decimal BuiltUpArea,
    decimal? LandArea,
    FinishingType? FinishingType,
    FurnishedStatus FurnishedStatus,
    UnitStatus Status,
    ListingUnitTypeSummary UnitType,
    ListingLocationSummary Location,
    ListingDetailsProject? Project);

public sealed record ListingMediaItem(
    Guid FileAssetId,
    int SortOrder,
    bool IsCover,
    string? Caption);

public sealed record ListingPaymentPlanItem(
    Guid Id,
    string Name,
    decimal TotalPrice,
    ListingCurrencySummary Currency,
    decimal DownPaymentPercentage,
    int DurationMonths,
    InstallmentFrequency InstallmentFrequency,
    decimal? CashDiscountPercentage);

public sealed record ListingAmenityItem(
    Guid Id,
    string Code,
    string NameEn,
    string NameAr,
    string? IconKey);

public sealed record ListingNearbyPlaceItem(
    Guid Id,
    string Category,
    string Name,
    decimal? DistanceMeters,
    int? TravelMinutes,
    decimal? Latitude,
    decimal? Longitude);

public sealed record ListingDetails(
    Guid Id,
    string Slug,
    string Title,
    string Description,
    ListingType ListingType,
    decimal AskingPrice,
    RentPeriod? RentPeriod,
    DateTimeOffset? PublishedAt,
    Guid? CoverFileAssetId,
    ListingCurrencySummary Currency,
    ListingDetailsCompany Company,
    ListingDetailsUnit Unit,
    IReadOnlyList<ListingMediaItem> Media,
    IReadOnlyList<ListingPaymentPlanItem> PaymentPlans,
    IReadOnlyList<ListingAmenityItem> UnitAmenities,
    IReadOnlyList<ListingAmenityItem> ProjectAmenities,
    IReadOnlyList<ListingNearbyPlaceItem> UnitNearbyPlaces,
    IReadOnlyList<ListingNearbyPlaceItem> ProjectNearbyPlaces);
