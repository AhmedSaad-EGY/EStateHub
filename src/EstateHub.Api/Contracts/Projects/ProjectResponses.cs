using EstateHub.Application.Common;
using EstateHub.Application.Projects;
using EstateHub.Domain.Enums;

namespace EstateHub.Api.Contracts.Projects;

public sealed record ProjectLocationResponse(
    Guid Id,
    string Type,
    string NameEn,
    string NameAr,
    string Slug)
{
    public static ProjectLocationResponse From(ProjectLocationSummary location)
    {
        return new ProjectLocationResponse(
            location.Id,
            ProjectEnumText.From(location.Type),
            location.NameEn,
            location.NameAr,
            location.Slug);
    }
}

public sealed record ProjectDeveloperResponse(
    Guid Id,
    string Slug,
    string DisplayName,
    Guid? LogoFileAssetId)
{
    public static ProjectDeveloperResponse From(ProjectDeveloperSummary company)
    {
        return new ProjectDeveloperResponse(
            company.Id,
            company.Slug,
            company.DisplayName,
            company.LogoFileAssetId);
    }
}

public sealed record ProjectDirectoryItemResponse(
    Guid Id,
    string Slug,
    string Name,
    string DeliveryStatus,
    DateOnly? ExpectedDeliveryDate,
    Guid? CoverFileAssetId,
    ProjectLocationResponse Location,
    int PublishedListingCount)
{
    public static ProjectDirectoryItemResponse From(ProjectDirectoryItem project)
    {
        return new ProjectDirectoryItemResponse(
            project.Id,
            project.Slug,
            project.Name,
            ProjectEnumText.From(project.DeliveryStatus),
            project.ExpectedDeliveryDate,
            project.CoverFileAssetId,
            ProjectLocationResponse.From(project.Location),
            project.PublishedListingCount);
    }
}

public sealed record ProjectDirectoryResponse(
    IReadOnlyList<ProjectDirectoryItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public static ProjectDirectoryResponse From(
        PagedResult<ProjectDirectoryItem> result)
    {
        return new ProjectDirectoryResponse(
            result.Items.Select(ProjectDirectoryItemResponse.From).ToArray(),
            result.PageNumber,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            result.HasPreviousPage,
            result.HasNextPage);
    }
}

public sealed record ProjectMediaResponse(
    Guid FileAssetId,
    int SortOrder,
    bool IsCover,
    string? Caption)
{
    public static ProjectMediaResponse From(ProjectMediaItem media)
    {
        return new ProjectMediaResponse(
            media.FileAssetId,
            media.SortOrder,
            media.IsCover,
            media.Caption);
    }
}

public sealed record ProjectAmenityResponse(
    Guid Id,
    string Code,
    string NameEn,
    string NameAr,
    string? IconKey)
{
    public static ProjectAmenityResponse From(ProjectAmenityItem amenity)
    {
        return new ProjectAmenityResponse(
            amenity.Id,
            amenity.Code,
            amenity.NameEn,
            amenity.NameAr,
            amenity.IconKey);
    }
}

public sealed record ProjectNearbyPlaceResponse(
    Guid Id,
    string Category,
    string Name,
    decimal? DistanceMeters,
    int? TravelMinutes,
    decimal? Latitude,
    decimal? Longitude)
{
    public static ProjectNearbyPlaceResponse From(ProjectNearbyPlaceItem place)
    {
        return new ProjectNearbyPlaceResponse(
            place.Id,
            place.Category,
            place.Name,
            place.DistanceMeters,
            place.TravelMinutes,
            place.Latitude,
            place.Longitude);
    }
}

public sealed record ProjectDetailsResponse(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    string DeliveryStatus,
    DateOnly? ExpectedDeliveryDate,
    Guid? CoverFileAssetId,
    int PublishedListingCount,
    ProjectDeveloperResponse DeveloperCompany,
    ProjectLocationResponse Location,
    IReadOnlyList<ProjectMediaResponse> Media,
    IReadOnlyList<ProjectAmenityResponse> Amenities,
    IReadOnlyList<ProjectNearbyPlaceResponse> NearbyPlaces)
{
    public static ProjectDetailsResponse From(ProjectDetails project)
    {
        return new ProjectDetailsResponse(
            project.Id,
            project.Slug,
            project.Name,
            project.Description,
            ProjectEnumText.From(project.DeliveryStatus),
            project.ExpectedDeliveryDate,
            project.CoverFileAssetId,
            project.PublishedListingCount,
            ProjectDeveloperResponse.From(project.DeveloperCompany),
            ProjectLocationResponse.From(project.Location),
            project.Media.Select(ProjectMediaResponse.From).ToArray(),
            project.Amenities.Select(ProjectAmenityResponse.From).ToArray(),
            project.NearbyPlaces.Select(ProjectNearbyPlaceResponse.From).ToArray());
    }
}

internal static class ProjectEnumText
{
    public static string From(ProjectDeliveryStatus deliveryStatus)
    {
        return deliveryStatus switch
        {
            ProjectDeliveryStatus.Planned => "Planned",
            ProjectDeliveryStatus.UnderConstruction => "UnderConstruction",
            ProjectDeliveryStatus.ReadyToMove => "ReadyToMove",
            ProjectDeliveryStatus.Delivered => "Delivered",
            _ => throw new ArgumentOutOfRangeException(
                nameof(deliveryStatus),
                deliveryStatus,
                "Unsupported project delivery status.")
        };
    }

    public static string From(LocationType locationType)
    {
        return locationType switch
        {
            LocationType.Country => "Country",
            LocationType.Governorate => "Governorate",
            LocationType.City => "City",
            LocationType.District => "District",
            _ => throw new ArgumentOutOfRangeException(
                nameof(locationType),
                locationType,
                "Unsupported location type.")
        };
    }
}
