using EstateHub.Application.Common;
using EstateHub.Application.Companies;
using EstateHub.Domain.Enums;

namespace EstateHub.Api.Contracts.Companies;

public sealed record CompanyLocationResponse(
    Guid Id,
    string Type,
    string NameEn,
    string NameAr,
    string Slug)
{
    public static CompanyLocationResponse From(CompanyLocationSummary location)
    {
        return new CompanyLocationResponse(
            location.Id,
            PublicCompanyEnumText.From(location.Type),
            location.NameEn,
            location.NameAr,
            location.Slug);
    }
}

public sealed record CompanyDirectoryItemResponse(
    Guid Id,
    string Slug,
    string DisplayName,
    string CompanyType,
    Guid? LogoFileAssetId,
    CompanyLocationResponse Location,
    int PublishedProjectCount,
    int PublishedListingCount)
{
    public static CompanyDirectoryItemResponse From(CompanyDirectoryItem company)
    {
        return new CompanyDirectoryItemResponse(
            company.Id,
            company.Slug,
            company.DisplayName,
            PublicCompanyEnumText.From(company.CompanyType),
            company.LogoFileAssetId,
            CompanyLocationResponse.From(company.Location),
            company.PublishedProjectCount,
            company.PublishedListingCount);
    }
}

public sealed record CompanyDirectoryResponse(
    IReadOnlyList<CompanyDirectoryItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public static CompanyDirectoryResponse From(
        PagedResult<CompanyDirectoryItem> result)
    {
        return new CompanyDirectoryResponse(
            result.Items.Select(CompanyDirectoryItemResponse.From).ToArray(),
            result.PageNumber,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            result.HasPreviousPage,
            result.HasNextPage);
    }
}

public sealed record CompanyAddressResponse(
    string AddressLine1,
    string? AddressLine2,
    string? PostalCode,
    decimal? Latitude,
    decimal? Longitude,
    CompanyLocationResponse Location)
{
    public static CompanyAddressResponse From(CompanyAddressDetails address)
    {
        return new CompanyAddressResponse(
            address.AddressLine1,
            address.AddressLine2,
            address.PostalCode,
            address.Latitude,
            address.Longitude,
            CompanyLocationResponse.From(address.Location));
    }
}

public sealed record CompanyDetailsResponse(
    Guid Id,
    string Slug,
    string DisplayName,
    string LegalName,
    string CompanyType,
    string BusinessEmail,
    string SupportPhone,
    string? Website,
    Guid? LogoFileAssetId,
    Guid? CoverFileAssetId,
    string TimeZoneId,
    string BaseCurrencyCode,
    DateTimeOffset VerifiedAt,
    CompanyAddressResponse Address,
    int PublishedProjectCount,
    int PublishedListingCount)
{
    public static CompanyDetailsResponse From(CompanyDetails company)
    {
        return new CompanyDetailsResponse(
            company.Id,
            company.Slug,
            company.DisplayName,
            company.LegalName,
            PublicCompanyEnumText.From(company.CompanyType),
            company.BusinessEmail,
            company.SupportPhone,
            company.Website,
            company.LogoFileAssetId,
            company.CoverFileAssetId,
            company.TimeZoneId,
            company.BaseCurrencyCode,
            company.VerifiedAt,
            CompanyAddressResponse.From(company.Address),
            company.PublishedProjectCount,
            company.PublishedListingCount);
    }
}

internal static class PublicCompanyEnumText
{
    public static string From(CompanyType companyType)
    {
        return companyType switch
        {
            CompanyType.Developer => "Developer",
            CompanyType.BrokerAgency => "BrokerAgency",
            _ => throw new ArgumentOutOfRangeException(
                nameof(companyType),
                companyType,
                "Unsupported company type.")
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
