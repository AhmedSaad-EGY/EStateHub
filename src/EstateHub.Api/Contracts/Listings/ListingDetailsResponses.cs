using EstateHub.Application.Listings;
using EstateHub.Domain.Enums;

namespace EstateHub.Api.Contracts.Listings;

public sealed record ListingDetailsCompanyResponse(
    Guid Id,
    string Slug,
    string DisplayName,
    Guid? LogoFileAssetId,
    string BusinessEmail,
    string SupportPhone,
    string? Website)
{
    public static ListingDetailsCompanyResponse From(ListingDetailsCompany company)
    {
        return new ListingDetailsCompanyResponse(
            company.Id,
            company.Slug,
            company.DisplayName,
            company.LogoFileAssetId,
            company.BusinessEmail,
            company.SupportPhone,
            company.Website);
    }
}

public sealed record ListingDetailsProjectDeveloperResponse(
    Guid Id,
    string Slug,
    string DisplayName,
    Guid? LogoFileAssetId)
{
    public static ListingDetailsProjectDeveloperResponse From(
        ListingDetailsProjectDeveloper company)
    {
        return new ListingDetailsProjectDeveloperResponse(
            company.Id,
            company.Slug,
            company.DisplayName,
            company.LogoFileAssetId);
    }
}

public sealed record ListingDetailsProjectResponse(
    Guid Id,
    string Slug,
    string Name,
    string DeliveryStatus,
    DateOnly? ExpectedDeliveryDate,
    ListingDetailsProjectDeveloperResponse DeveloperCompany)
{
    public static ListingDetailsProjectResponse From(ListingDetailsProject project)
    {
        return new ListingDetailsProjectResponse(
            project.Id,
            project.Slug,
            project.Name,
            ListingDetailsEnumText.From(project.DeliveryStatus),
            project.ExpectedDeliveryDate,
            ListingDetailsProjectDeveloperResponse.From(project.DeveloperCompany));
    }
}

public sealed record ListingDetailsUnitResponse(
    Guid Id,
    int Bedrooms,
    int Bathrooms,
    int? FloorNumber,
    int? TotalFloors,
    decimal BuiltUpArea,
    decimal? LandArea,
    string? FinishingType,
    string FurnishedStatus,
    string Status,
    ListingUnitTypeResponse UnitType,
    ListingLocationResponse Location,
    ListingDetailsProjectResponse? Project)
{
    public static ListingDetailsUnitResponse From(ListingDetailsUnit unit)
    {
        return new ListingDetailsUnitResponse(
            unit.Id,
            unit.Bedrooms,
            unit.Bathrooms,
            unit.FloorNumber,
            unit.TotalFloors,
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
                : ListingDetailsProjectResponse.From(unit.Project));
    }
}

public sealed record ListingMediaResponse(
    Guid FileAssetId,
    int SortOrder,
    bool IsCover,
    string? Caption)
{
    public static ListingMediaResponse From(ListingMediaItem media)
    {
        return new ListingMediaResponse(
            media.FileAssetId,
            media.SortOrder,
            media.IsCover,
            media.Caption);
    }
}

public sealed record ListingPaymentPlanResponse(
    Guid Id,
    string Name,
    decimal TotalPrice,
    ListingCurrencyResponse Currency,
    decimal DownPaymentPercentage,
    int DurationMonths,
    string InstallmentFrequency,
    decimal? CashDiscountPercentage)
{
    public static ListingPaymentPlanResponse From(ListingPaymentPlanItem plan)
    {
        return new ListingPaymentPlanResponse(
            plan.Id,
            plan.Name,
            plan.TotalPrice,
            ListingCurrencyResponse.From(plan.Currency),
            plan.DownPaymentPercentage,
            plan.DurationMonths,
            ListingDetailsEnumText.From(plan.InstallmentFrequency),
            plan.CashDiscountPercentage);
    }
}

public sealed record ListingAmenityResponse(
    Guid Id,
    string Code,
    string NameEn,
    string NameAr,
    string? IconKey)
{
    public static ListingAmenityResponse From(ListingAmenityItem amenity)
    {
        return new ListingAmenityResponse(
            amenity.Id,
            amenity.Code,
            amenity.NameEn,
            amenity.NameAr,
            amenity.IconKey);
    }
}

public sealed record ListingNearbyPlaceResponse(
    Guid Id,
    string Category,
    string Name,
    decimal? DistanceMeters,
    int? TravelMinutes,
    decimal? Latitude,
    decimal? Longitude)
{
    public static ListingNearbyPlaceResponse From(ListingNearbyPlaceItem place)
    {
        return new ListingNearbyPlaceResponse(
            place.Id,
            place.Category,
            place.Name,
            place.DistanceMeters,
            place.TravelMinutes,
            place.Latitude,
            place.Longitude);
    }
}

public sealed record ListingDetailsResponse(
    Guid Id,
    string Slug,
    string Title,
    string Description,
    string ListingType,
    decimal AskingPrice,
    string? RentPeriod,
    DateTimeOffset? PublishedAt,
    Guid? CoverFileAssetId,
    ListingCurrencyResponse Currency,
    ListingDetailsCompanyResponse Company,
    ListingDetailsUnitResponse Unit,
    IReadOnlyList<ListingMediaResponse> Media,
    IReadOnlyList<ListingPaymentPlanResponse> PaymentPlans,
    IReadOnlyList<ListingAmenityResponse> UnitAmenities,
    IReadOnlyList<ListingAmenityResponse> ProjectAmenities,
    IReadOnlyList<ListingNearbyPlaceResponse> UnitNearbyPlaces,
    IReadOnlyList<ListingNearbyPlaceResponse> ProjectNearbyPlaces)
{
    public static ListingDetailsResponse From(ListingDetails listing)
    {
        return new ListingDetailsResponse(
            listing.Id,
            listing.Slug,
            listing.Title,
            listing.Description,
            ListingEnumText.From(listing.ListingType),
            listing.AskingPrice,
            listing.RentPeriod is null
                ? null
                : ListingEnumText.From(listing.RentPeriod.Value),
            listing.PublishedAt,
            listing.CoverFileAssetId,
            ListingCurrencyResponse.From(listing.Currency),
            ListingDetailsCompanyResponse.From(listing.Company),
            ListingDetailsUnitResponse.From(listing.Unit),
            listing.Media.Select(ListingMediaResponse.From).ToArray(),
            listing.PaymentPlans.Select(ListingPaymentPlanResponse.From).ToArray(),
            listing.UnitAmenities.Select(ListingAmenityResponse.From).ToArray(),
            listing.ProjectAmenities.Select(ListingAmenityResponse.From).ToArray(),
            listing.UnitNearbyPlaces.Select(ListingNearbyPlaceResponse.From).ToArray(),
            listing.ProjectNearbyPlaces.Select(ListingNearbyPlaceResponse.From).ToArray());
    }
}

internal static class ListingDetailsEnumText
{
    public static string From(ProjectDeliveryStatus value)
    {
        return value switch
        {
            ProjectDeliveryStatus.Planned => "Planned",
            ProjectDeliveryStatus.UnderConstruction => "UnderConstruction",
            ProjectDeliveryStatus.ReadyToMove => "ReadyToMove",
            ProjectDeliveryStatus.Delivered => "Delivered",
            _ => Unsupported(value)
        };
    }

    public static string From(InstallmentFrequency value)
    {
        return value switch
        {
            InstallmentFrequency.Monthly => "Monthly",
            InstallmentFrequency.Quarterly => "Quarterly",
            InstallmentFrequency.SemiAnnual => "SemiAnnual",
            InstallmentFrequency.Annual => "Annual",
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
