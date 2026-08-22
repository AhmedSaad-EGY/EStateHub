using EstateHub.Application.Common;
using EstateHub.Domain.Enums;

namespace EstateHub.Application.CompanyUnits;

public sealed record CompanyUnitLocationSummary(Guid Id, LocationType Type, string NameEn, string NameAr, string Slug);
public sealed record CompanyUnitTypeSummary(Guid Id, string Code, string NameEn, string NameAr);
public sealed record CompanyUnitProjectSummary(Guid Id, string Slug, string Name, ProjectDeliveryStatus DeliveryStatus, ProjectStatus Status);
public sealed record CompanyUnitDetails(Guid Id, Guid? ProjectId, string UnitCode, FinishingType? FinishingType, int Bedrooms, int Bathrooms, int? FloorNumber, int? TotalFloors, decimal BuiltUpArea, decimal? LandArea, FurnishedStatus FurnishedStatus, UnitStatus Status, CompanyUnitLocationSummary Location, CompanyUnitTypeSummary UnitType, CompanyUnitProjectSummary? Project, int ListingCount, int PublishedListingCount, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, byte[] RowVersion);
public sealed record CompanyUnitDirectoryQuery(int PageNumber, int PageSize, string? Search, UnitStatus? Status, Guid? ProjectId, Guid? UnitTypeId, Guid? LocationId);
public sealed record CompanyUnitCommand(Guid? ProjectId, Guid LocationId, Guid UnitTypeId, string UnitCode, FinishingType? FinishingType, int Bedrooms, int Bathrooms, int? FloorNumber, int? TotalFloors, decimal BuiltUpArea, decimal? LandArea, FurnishedStatus FurnishedStatus, byte[]? RowVersion);
public enum CompanyUnitOperationStatus { Succeeded, NotFound, Conflict, InvalidReference, ServiceUnavailable }
public sealed record CompanyUnitMutationResult(CompanyUnitOperationStatus Status, CompanyUnitDetails? Unit = null);
public sealed record CompanyUnitStatusMutationResult(CompanyUnitOperationStatus Status, UnitStatus? UnitStatus = null, DateTimeOffset? UpdatedAt = null, byte[]? RowVersion = null);
public sealed record CompanyUnitAmenity(Guid Id, string Code, string NameEn, string NameAr, string? IconKey, AmenityScope Scope);
public sealed record CompanyUnitAmenitiesResult(CompanyUnitOperationStatus Status, byte[]? RowVersion = null, IReadOnlyList<CompanyUnitAmenity>? Amenities = null);
public sealed record CompanyUnitNearbyPlaceCommand(string Category, string Name, decimal? DistanceMeters, int? TravelMinutes, decimal? Latitude, decimal? Longitude);
public sealed record CompanyUnitNearbyPlace(Guid Id, string Category, string Name, decimal? DistanceMeters, int? TravelMinutes, decimal? Latitude, decimal? Longitude);
public sealed record CompanyUnitNearbyPlaceResult(CompanyUnitOperationStatus Status, byte[]? RowVersion = null, CompanyUnitNearbyPlace? NearbyPlace = null);
public sealed record CompanyUnitManagementAmenity(Guid Id, string Code, string NameEn, string NameAr, string? IconKey, AmenityScope Scope, bool IsActive);
public sealed record CompanyUnitManagementDetails(CompanyUnitDetails Header, IReadOnlyList<CompanyUnitManagementAmenity> Amenities, IReadOnlyList<CompanyUnitNearbyPlace> NearbyPlaces);
