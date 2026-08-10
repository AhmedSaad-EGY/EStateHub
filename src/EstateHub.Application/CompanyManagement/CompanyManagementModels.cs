using EstateHub.Application.Common;
using EstateHub.Domain.Enums;

namespace EstateHub.Application.CompanyManagement;

public sealed record CompanyLocationSummary(
    Guid Id,
    LocationType Type,
    string NameEn,
    string NameAr,
    string Slug);

public sealed record CompanyAddressDetails(
    Guid Id,
    CompanyLocationSummary Location,
    string AddressLine1,
    string? AddressLine2,
    string? PostalCode,
    decimal? Latitude,
    decimal? Longitude);

public sealed record CompanyProfileDetails(
    Guid Id,
    string Slug,
    string LegalName,
    string DisplayName,
    string RegistrationNumber,
    string? TaxId,
    CompanyType CompanyType,
    string BusinessEmail,
    string SupportPhone,
    string? Website,
    Guid? LogoFileAssetId,
    Guid? CoverFileAssetId,
    CompanyStatus Status,
    DateTimeOffset? VerifiedAt,
    string TimeZoneId,
    string BaseCurrencyCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    byte[] RowVersion,
    CompanyAddressDetails Address);

public sealed record UpdateCompanyProfileCommand(
    string DisplayName,
    string BusinessEmail,
    string SupportPhone,
    string? Website,
    string TimeZoneId,
    string BaseCurrencyCode,
    Guid AddressLocationId,
    string AddressLine1,
    string? AddressLine2,
    string? PostalCode,
    decimal? Latitude,
    decimal? Longitude,
    byte[] RowVersion);

public sealed record CompanyEmployeeRoleItem(
    Guid Id,
    string Name,
    bool IsBuiltIn);

public sealed record CompanyEmployeeDetails(
    Guid Id,
    string Email,
    string FullName,
    string? JobTitle,
    bool IsPrimaryContact,
    CompanyEmployeeStatus Status,
    DateTimeOffset JoinedAt,
    DateTimeOffset? EndedAt,
    byte[] RowVersion,
    IReadOnlyList<CompanyEmployeeRoleItem> ActiveRoles);

public sealed record EmployeeDirectoryQuery(
    int PageNumber,
    int PageSize,
    string? Search,
    CompanyEmployeeStatus? Status,
    bool IncludeEnded);

public sealed record AddCompanyEmployeeCommand(
    string Email,
    string FullName,
    string? JobTitle);

public sealed record UpdateCompanyEmployeeCommand(
    string FullName,
    string? JobTitle,
    byte[] RowVersion);

public sealed record EmployeeRowVersionCommand(byte[] RowVersion);

public sealed record ReplaceCompanyEmployeeRolesCommand(
    IReadOnlyList<Guid> RoleIds);

public sealed record TransferPrimaryContactCommand(byte[] TargetEmployeeRowVersion);

public enum CompanyManagementStatus
{
    Succeeded,
    NotFound,
    Conflict,
    InvalidReference,
    ServiceUnavailable
}

public sealed record CompanyProfileMutationResult(
    CompanyManagementStatus Status,
    CompanyProfileDetails? Profile = null);

public sealed record CompanyEmployeeMutationResult(
    CompanyManagementStatus Status,
    CompanyEmployeeDetails? Employee = null);
