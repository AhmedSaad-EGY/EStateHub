using EstateHub.Domain.Enums;

namespace EstateHub.Application.Projects;

public sealed record ProjectDirectoryQuery(
    int PageNumber,
    int PageSize,
    string? Search,
    ProjectDeliveryStatus? DeliveryStatus,
    Guid? LocationId);

public sealed record ProjectLocationSummary(
    Guid Id,
    LocationType Type,
    string NameEn,
    string NameAr,
    string Slug);

public sealed record ProjectDeveloperSummary(
    Guid Id,
    string Slug,
    string DisplayName,
    Guid? LogoFileAssetId);

public sealed record ProjectDirectoryItem(
    Guid Id,
    string Slug,
    string Name,
    ProjectDeliveryStatus DeliveryStatus,
    DateOnly? ExpectedDeliveryDate,
    Guid? CoverFileAssetId,
    ProjectLocationSummary Location,
    int PublishedListingCount);

public sealed record ProjectMediaItem(
    Guid FileAssetId,
    int SortOrder,
    bool IsCover,
    string? Caption);

public sealed record ProjectAmenityItem(
    Guid Id,
    string Code,
    string NameEn,
    string NameAr,
    string? IconKey);

public sealed record ProjectNearbyPlaceItem(
    Guid Id,
    string Category,
    string Name,
    decimal? DistanceMeters,
    int? TravelMinutes,
    decimal? Latitude,
    decimal? Longitude);

public sealed record ProjectDetails(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    ProjectDeliveryStatus DeliveryStatus,
    DateOnly? ExpectedDeliveryDate,
    Guid? CoverFileAssetId,
    int PublishedListingCount,
    ProjectDeveloperSummary DeveloperCompany,
    ProjectLocationSummary Location,
    IReadOnlyList<ProjectMediaItem> Media,
    IReadOnlyList<ProjectAmenityItem> Amenities,
    IReadOnlyList<ProjectNearbyPlaceItem> NearbyPlaces);
