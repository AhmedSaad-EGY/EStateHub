using EstateHub.Application.Common;
using EstateHub.Domain.Enums;
namespace EstateHub.Application.CompanyProjects;
public sealed record CompanyProjectLocation(Guid Id,LocationType Type,string NameEn,string NameAr,string Slug);
public sealed record CompanyProjectSummary(Guid Id,string Name,string Slug,string? Description,ProjectDeliveryStatus DeliveryStatus,DateOnly? ExpectedDeliveryDate,ProjectStatus ProjectStatus,CompanyProjectLocation Location,Guid? CoverFileAssetId,int UnitCount,int PublishedListingCount,DateTimeOffset CreatedAt,DateTimeOffset UpdatedAt,byte[] RowVersion);
public sealed record CompanyProjectQuery(int PageNumber,int PageSize,string? Search,ProjectStatus? Status,ProjectDeliveryStatus? DeliveryStatus,Guid? LocationId);
public sealed record CompanyProjectCommand(string Name,string Slug,string? Description,Guid LocationId,ProjectDeliveryStatus DeliveryStatus,DateOnly? ExpectedDeliveryDate,byte[]? RowVersion);
public enum CompanyProjectOperationStatus { Succeeded,NotFound,Conflict,InvalidReference,ServiceUnavailable }
public sealed record CompanyProjectOperationResult(CompanyProjectOperationStatus Status,CompanyProjectSummary? Project=null);
public sealed record CompanyProjectAmenity(Guid Id,string Code,string NameEn,string NameAr,string? IconKey,AmenityScope Scope);
public sealed record CompanyProjectMedia(Guid FileAssetId,int SortOrder,bool IsCover,string? Caption);
public sealed record CompanyProjectAmenitiesResult(CompanyProjectOperationStatus Status,byte[]? RowVersion=null,IReadOnlyList<CompanyProjectAmenity>? Amenities=null);
public sealed record CompanyProjectMediaResult(CompanyProjectOperationStatus Status,byte[]? RowVersion=null,IReadOnlyList<CompanyProjectMedia>? Media=null);
public sealed record CompanyProjectNearbyPlaceCommand(string Category,string Name,decimal? DistanceMeters,int? TravelMinutes,decimal? Latitude,decimal? Longitude);
public sealed record CompanyProjectNearbyPlace(Guid Id,string Category,string Name,decimal? DistanceMeters,int? TravelMinutes,decimal? Latitude,decimal? Longitude);
public sealed record CompanyProjectNearbyPlaceResult(CompanyProjectOperationStatus Status,byte[]? RowVersion=null,CompanyProjectNearbyPlace? NearbyPlace=null);
public sealed record CompanyProjectManagementAmenity(Guid Id,string Code,string NameEn,string NameAr,string? IconKey,AmenityScope Scope,bool IsActive);
public sealed record CompanyProjectManagementNearbyPlace(Guid Id,string Category,string Name,decimal? DistanceMeters,int? TravelMinutes,decimal? Latitude,decimal? Longitude);
public sealed record CompanyProjectManagementDetails(CompanyProjectSummary Header,IReadOnlyList<CompanyProjectManagementAmenity> Amenities,IReadOnlyList<CompanyProjectMedia> Media,IReadOnlyList<CompanyProjectManagementNearbyPlace> NearbyPlaces);
