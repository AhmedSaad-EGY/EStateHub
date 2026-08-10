using EstateHub.Application.Common;
using EstateHub.Application.CompanyManagement;
using EstateHub.Domain.Enums;

namespace EstateHub.Api.Contracts.CompanyManagement;

public sealed record CompanyLocationResponse(
    Guid Id,
    string Type,
    string NameEn,
    string NameAr,
    string Slug)
{
    public static CompanyLocationResponse From(CompanyLocationSummary location) =>
        new(
            location.Id,
            ToText(location.Type),
            location.NameEn,
            location.NameAr,
            location.Slug);

    private static string ToText(LocationType value) => value switch
    {
        LocationType.Country => "Country",
        LocationType.Governorate => "Governorate",
        LocationType.City => "City",
        LocationType.District => "District",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}

public sealed record CompanyAddressResponse(
    Guid Id,
    CompanyLocationResponse Location,
    string AddressLine1,
    string? AddressLine2,
    string? PostalCode,
    decimal? Latitude,
    decimal? Longitude)
{
    public static CompanyAddressResponse From(CompanyAddressDetails address) =>
        new(
            address.Id,
            CompanyLocationResponse.From(address.Location),
            address.AddressLine1,
            address.AddressLine2,
            address.PostalCode,
            address.Latitude,
            address.Longitude);
}

public sealed record CompanyProfileResponse(
    Guid Id,
    string Slug,
    string LegalName,
    string DisplayName,
    string RegistrationNumber,
    string? TaxId,
    string CompanyType,
    string BusinessEmail,
    string SupportPhone,
    string? Website,
    Guid? LogoFileAssetId,
    Guid? CoverFileAssetId,
    string Status,
    DateTimeOffset? VerifiedAt,
    string TimeZoneId,
    string BaseCurrencyCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string RowVersion,
    CompanyAddressResponse Address)
{
    public static CompanyProfileResponse From(CompanyProfileDetails profile) =>
        new(
            profile.Id,
            profile.Slug,
            profile.LegalName,
            profile.DisplayName,
            profile.RegistrationNumber,
            profile.TaxId,
            ToText(profile.CompanyType),
            profile.BusinessEmail,
            profile.SupportPhone,
            profile.Website,
            profile.LogoFileAssetId,
            profile.CoverFileAssetId,
            ToText(profile.Status),
            profile.VerifiedAt,
            profile.TimeZoneId,
            profile.BaseCurrencyCode,
            profile.CreatedAt,
            profile.UpdatedAt,
            Convert.ToBase64String(profile.RowVersion),
            CompanyAddressResponse.From(profile.Address));

    private static string ToText(EstateHub.Domain.Enums.CompanyType value) => value switch
    {
        EstateHub.Domain.Enums.CompanyType.Developer => "Developer",
        EstateHub.Domain.Enums.CompanyType.BrokerAgency => "BrokerAgency",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static string ToText(CompanyStatus value) => value switch
    {
        CompanyStatus.Active => "Active",
        CompanyStatus.Suspended => "Suspended",
        CompanyStatus.Deactivated => "Deactivated",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}

public sealed record CompanyEmployeeRoleResponse(
    Guid Id,
    string Name,
    bool IsBuiltIn)
{
    public static CompanyEmployeeRoleResponse From(CompanyEmployeeRoleItem role) =>
        new(role.Id, role.Name, role.IsBuiltIn);
}

public sealed record CompanyEmployeeResponse(
    Guid Id,
    string Email,
    string FullName,
    string? JobTitle,
    bool IsPrimaryContact,
    string Status,
    DateTimeOffset JoinedAt,
    DateTimeOffset? EndedAt,
    string RowVersion,
    IReadOnlyList<CompanyEmployeeRoleResponse> ActiveRoles)
{
    public static CompanyEmployeeResponse From(CompanyEmployeeDetails employee) =>
        new(
            employee.Id,
            employee.Email,
            employee.FullName,
            employee.JobTitle,
            employee.IsPrimaryContact,
            ToText(employee.Status),
            employee.JoinedAt,
            employee.EndedAt,
            Convert.ToBase64String(employee.RowVersion),
            employee.ActiveRoles.Select(CompanyEmployeeRoleResponse.From).ToList());

    private static string ToText(CompanyEmployeeStatus value) => value switch
    {
        CompanyEmployeeStatus.Active => "Active",
        CompanyEmployeeStatus.Suspended => "Suspended",
        CompanyEmployeeStatus.Ended => "Ended",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}

public sealed record CompanyEmployeesResponse(
    IReadOnlyList<CompanyEmployeeResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public static CompanyEmployeesResponse From(
        PagedResult<CompanyEmployeeDetails> result) =>
        new(
            result.Items.Select(CompanyEmployeeResponse.From).ToList(),
            result.PageNumber,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            result.HasPreviousPage,
            result.HasNextPage);
}
