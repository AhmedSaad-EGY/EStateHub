using EstateHub.Domain.Enums;

namespace EstateHub.Application.Companies;

public sealed record CompanyDirectoryQuery(
    int PageNumber,
    int PageSize,
    string? Search,
    CompanyType? CompanyType,
    Guid? LocationId);

public sealed record CompanyLocationSummary(
    Guid Id,
    LocationType Type,
    string NameEn,
    string NameAr,
    string Slug);

public sealed record CompanyDirectoryItem(
    Guid Id,
    string Slug,
    string DisplayName,
    CompanyType CompanyType,
    Guid? LogoFileAssetId,
    CompanyLocationSummary Location,
    int PublishedProjectCount,
    int PublishedListingCount);

public sealed record CompanyAddressDetails(
    string AddressLine1,
    string? AddressLine2,
    string? PostalCode,
    decimal? Latitude,
    decimal? Longitude,
    CompanyLocationSummary Location);

public sealed record CompanyDetails(
    Guid Id,
    string Slug,
    string DisplayName,
    string LegalName,
    CompanyType CompanyType,
    string BusinessEmail,
    string SupportPhone,
    string? Website,
    Guid? LogoFileAssetId,
    Guid? CoverFileAssetId,
    string TimeZoneId,
    string BaseCurrencyCode,
    DateTimeOffset VerifiedAt,
    CompanyAddressDetails Address,
    int PublishedProjectCount,
    int PublishedListingCount);
