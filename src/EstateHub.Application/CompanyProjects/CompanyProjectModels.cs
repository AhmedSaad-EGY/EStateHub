using EstateHub.Application.Common;
using EstateHub.Domain.Enums;
namespace EstateHub.Application.CompanyProjects;
public sealed record CompanyProjectLocation(Guid Id,LocationType Type,string NameEn,string NameAr,string Slug);
public sealed record CompanyProjectSummary(Guid Id,string Name,string Slug,string? Description,ProjectDeliveryStatus DeliveryStatus,DateOnly? ExpectedDeliveryDate,ProjectStatus ProjectStatus,CompanyProjectLocation Location,Guid? CoverFileAssetId,int UnitCount,int PublishedListingCount,DateTimeOffset CreatedAt,DateTimeOffset UpdatedAt,byte[] RowVersion);
public sealed record CompanyProjectQuery(int PageNumber,int PageSize,string? Search,ProjectStatus? Status,ProjectDeliveryStatus? DeliveryStatus,Guid? LocationId);
public sealed record CompanyProjectCommand(string Name,string Slug,string? Description,Guid LocationId,ProjectDeliveryStatus DeliveryStatus,DateOnly? ExpectedDeliveryDate,byte[]? RowVersion);
public enum CompanyProjectOperationStatus { Succeeded,NotFound,Conflict,InvalidReference,ServiceUnavailable }
public sealed record CompanyProjectOperationResult(CompanyProjectOperationStatus Status,CompanyProjectSummary? Project=null);
