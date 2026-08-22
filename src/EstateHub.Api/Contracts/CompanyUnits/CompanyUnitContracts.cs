using EstateHub.Application.Common;
using EstateHub.Application.CompanyUnits;
using EstateHub.Domain.Enums;

namespace EstateHub.Api.Contracts.CompanyUnits;

public sealed class CompanyUnitDirectoryRequest { public int PageNumber { get; init; } = 1; public int PageSize { get; init; } = 20; public string? Search { get; init; } public string? Status { get; init; } public Guid? ProjectId { get; init; } public Guid? UnitTypeId { get; init; } public Guid? LocationId { get; init; } }
public sealed record CreateCompanyUnitRequest(Guid? ProjectId, Guid LocationId, Guid UnitTypeId, string? UnitCode, string? FinishingType, int Bedrooms, int Bathrooms, int? FloorNumber, int? TotalFloors, decimal BuiltUpArea, decimal? LandArea, string? FurnishedStatus);
public sealed record UpdateCompanyUnitRequest(Guid? ProjectId, Guid LocationId, Guid UnitTypeId, string? UnitCode, string? FinishingType, int Bedrooms, int Bathrooms, int? FloorNumber, int? TotalFloors, decimal BuiltUpArea, decimal? LandArea, string? FurnishedStatus, string? RowVersion);
public sealed record UpdateCompanyUnitStatusRequest(string? Status, string? RowVersion);
public sealed record CompanyUnitLocationResponse(Guid Id, string Type, string NameEn, string NameAr, string Slug) { public static CompanyUnitLocationResponse From(CompanyUnitLocationSummary x) => new(x.Id, Text(x.Type), x.NameEn, x.NameAr, x.Slug); static string Text(LocationType x) => x switch { LocationType.Country => "Country", LocationType.Governorate => "Governorate", LocationType.City => "City", LocationType.District => "District", _ => throw new ArgumentOutOfRangeException(nameof(x), x, null) }; }
public sealed record CompanyUnitTypeResponse(Guid Id, string Code, string NameEn, string NameAr) { public static CompanyUnitTypeResponse From(CompanyUnitTypeSummary x) => new(x.Id, x.Code, x.NameEn, x.NameAr); }
public sealed record CompanyUnitProjectResponse(Guid Id, string Slug, string Name, string DeliveryStatus, string Status) { public static CompanyUnitProjectResponse From(CompanyUnitProjectSummary x) => new(x.Id, x.Slug, x.Name, Text(x.DeliveryStatus), Text(x.Status)); static string Text(ProjectDeliveryStatus x) => x switch { ProjectDeliveryStatus.Planned => "Planned", ProjectDeliveryStatus.UnderConstruction => "UnderConstruction", ProjectDeliveryStatus.ReadyToMove => "ReadyToMove", ProjectDeliveryStatus.Delivered => "Delivered", _ => throw new ArgumentOutOfRangeException(nameof(x), x, null) }; static string Text(ProjectStatus x) => x switch { ProjectStatus.Draft => "Draft", ProjectStatus.Published => "Published", ProjectStatus.Archived => "Archived", _ => throw new ArgumentOutOfRangeException(nameof(x), x, null) }; }
public sealed record CompanyUnitListItemResponse(Guid Id, Guid? ProjectId, string UnitCode, string? FinishingType, int Bedrooms, int Bathrooms, int? FloorNumber, int? TotalFloors, decimal BuiltUpArea, decimal? LandArea, string FurnishedStatus, string Status, CompanyUnitLocationResponse Location, CompanyUnitTypeResponse UnitType, CompanyUnitProjectResponse? Project, int ListingCount, int PublishedListingCount, DateTimeOffset UpdatedAt, string RowVersion)
{
    public static CompanyUnitListItemResponse From(CompanyUnitDetails x) => new(x.Id, x.ProjectId, x.UnitCode, x.FinishingType is null ? null : Text(x.FinishingType.Value), x.Bedrooms, x.Bathrooms, x.FloorNumber, x.TotalFloors, x.BuiltUpArea, x.LandArea, Text(x.FurnishedStatus), Text(x.Status), CompanyUnitLocationResponse.From(x.Location), CompanyUnitTypeResponse.From(x.UnitType), x.Project is null ? null : CompanyUnitProjectResponse.From(x.Project), x.ListingCount, x.PublishedListingCount, x.UpdatedAt, Convert.ToBase64String(x.RowVersion));
    static string Text(global::EstateHub.Domain.Enums.FinishingType x) => x switch { global::EstateHub.Domain.Enums.FinishingType.Unfinished => "Unfinished", global::EstateHub.Domain.Enums.FinishingType.SemiFinished => "SemiFinished", global::EstateHub.Domain.Enums.FinishingType.Finished => "Finished", _ => throw new ArgumentOutOfRangeException(nameof(x), x, null) };
    static string Text(global::EstateHub.Domain.Enums.FurnishedStatus x) => x switch { global::EstateHub.Domain.Enums.FurnishedStatus.Unfurnished => "Unfurnished", global::EstateHub.Domain.Enums.FurnishedStatus.SemiFurnished => "SemiFurnished", global::EstateHub.Domain.Enums.FurnishedStatus.Furnished => "Furnished", _ => throw new ArgumentOutOfRangeException(nameof(x), x, null) };
    static string Text(UnitStatus x) => x switch { UnitStatus.Available => "Available", UnitStatus.Reserved => "Reserved", UnitStatus.Sold => "Sold", UnitStatus.Rented => "Rented", UnitStatus.Withdrawn => "Withdrawn", _ => throw new ArgumentOutOfRangeException(nameof(x), x, null) };
}
public sealed record CompanyUnitDetailsResponse(Guid Id, Guid? ProjectId, string UnitCode, string? FinishingType, int Bedrooms, int Bathrooms, int? FloorNumber, int? TotalFloors, decimal BuiltUpArea, decimal? LandArea, string FurnishedStatus, string Status, CompanyUnitLocationResponse Location, CompanyUnitTypeResponse UnitType, CompanyUnitProjectResponse? Project, int ListingCount, int PublishedListingCount, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string RowVersion)
{
    public static CompanyUnitDetailsResponse From(CompanyUnitDetails x) { var item = CompanyUnitListItemResponse.From(x); return new(item.Id, item.ProjectId, item.UnitCode, item.FinishingType, item.Bedrooms, item.Bathrooms, item.FloorNumber, item.TotalFloors, item.BuiltUpArea, item.LandArea, item.FurnishedStatus, item.Status, item.Location, item.UnitType, item.Project, item.ListingCount, item.PublishedListingCount, x.CreatedAt, item.UpdatedAt, item.RowVersion); }
}
public sealed record CompanyUnitsResponse(IReadOnlyList<CompanyUnitListItemResponse> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages, bool HasPreviousPage, bool HasNextPage) { public static CompanyUnitsResponse From(PagedResult<CompanyUnitDetails> x) => new(x.Items.Select(CompanyUnitListItemResponse.From).ToList(), x.PageNumber, x.PageSize, x.TotalCount, x.TotalPages, x.HasPreviousPage, x.HasNextPage); }
public sealed record CompanyUnitStatusResponse(string Status, DateTimeOffset UpdatedAt, string RowVersion);
public sealed record ReplaceCompanyUnitAmenitiesRequest(string? RowVersion, IReadOnlyList<Guid>? AmenityIds);
public sealed record CompanyUnitAmenityResponse(Guid Id, string Code, string NameEn, string NameAr, string? IconKey, string Scope)
{
    public static CompanyUnitAmenityResponse From(CompanyUnitAmenity x) => new(x.Id, x.Code, x.NameEn, x.NameAr, x.IconKey, x.Scope switch
    {
        AmenityScope.Unit => "Unit",
        AmenityScope.Both => "Both",
        AmenityScope.Project => "Project",
        _ => throw new ArgumentOutOfRangeException(nameof(x.Scope), x.Scope, null)
    });
}
public sealed record ReplaceCompanyUnitAmenitiesResponse(string RowVersion, IReadOnlyList<CompanyUnitAmenityResponse> Amenities);
public sealed record CompanyUnitNearbyPlaceRequest(string? Category, string? Name, decimal? DistanceMeters, int? TravelMinutes, decimal? Latitude, decimal? Longitude, string? RowVersion);
public sealed record CompanyUnitNearbyPlaceResponse(Guid Id, string Category, string Name, decimal? DistanceMeters, int? TravelMinutes, decimal? Latitude, decimal? Longitude)
{
    public static CompanyUnitNearbyPlaceResponse From(CompanyUnitNearbyPlace x) => new(x.Id, x.Category, x.Name, x.DistanceMeters, x.TravelMinutes, x.Latitude, x.Longitude);
}
public sealed record CompanyUnitNearbyPlaceMutationResponse(string RowVersion, CompanyUnitNearbyPlaceResponse NearbyPlace);
public sealed record CompanyUnitManagementAmenityResponse(Guid Id, string Code, string NameEn, string NameAr, string? IconKey, string Scope, bool IsActive)
{
    public static CompanyUnitManagementAmenityResponse From(CompanyUnitManagementAmenity x) => new(x.Id, x.Code, x.NameEn, x.NameAr, x.IconKey, x.Scope switch
    {
        AmenityScope.Project => "Project",
        AmenityScope.Unit => "Unit",
        AmenityScope.Both => "Both",
        _ => throw new ArgumentOutOfRangeException(nameof(x.Scope), x.Scope, null)
    }, x.IsActive);
}
public sealed record CompanyUnitManagementDetailsResponse(Guid Id, Guid? ProjectId, string UnitCode, string? FinishingType, int Bedrooms, int Bathrooms, int? FloorNumber, int? TotalFloors, decimal BuiltUpArea, decimal? LandArea, string FurnishedStatus, string Status, CompanyUnitLocationResponse Location, CompanyUnitTypeResponse UnitType, CompanyUnitProjectResponse? Project, int ListingCount, int PublishedListingCount, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string RowVersion, IReadOnlyList<CompanyUnitManagementAmenityResponse> Amenities, IReadOnlyList<CompanyUnitNearbyPlaceResponse> NearbyPlaces)
{
    public static CompanyUnitManagementDetailsResponse From(CompanyUnitManagementDetails x) { var h = CompanyUnitDetailsResponse.From(x.Header); return new(h.Id, h.ProjectId, h.UnitCode, h.FinishingType, h.Bedrooms, h.Bathrooms, h.FloorNumber, h.TotalFloors, h.BuiltUpArea, h.LandArea, h.FurnishedStatus, h.Status, h.Location, h.UnitType, h.Project, h.ListingCount, h.PublishedListingCount, h.CreatedAt, h.UpdatedAt, h.RowVersion, x.Amenities.Select(CompanyUnitManagementAmenityResponse.From).ToList(), x.NearbyPlaces.Select(CompanyUnitNearbyPlaceResponse.From).ToList()); }
}
