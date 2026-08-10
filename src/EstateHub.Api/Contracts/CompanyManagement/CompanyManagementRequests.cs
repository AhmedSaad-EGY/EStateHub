namespace EstateHub.Api.Contracts.CompanyManagement;

public sealed record UpdateCompanyProfileRequest(
    string? DisplayName,
    string? BusinessEmail,
    string? SupportPhone,
    string? Website,
    string? TimeZoneId,
    string? BaseCurrencyCode,
    Guid AddressLocationId,
    string? AddressLine1,
    string? AddressLine2,
    string? PostalCode,
    decimal? Latitude,
    decimal? Longitude,
    string? RowVersion);

public sealed class EmployeeDirectoryRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? Status { get; init; }
    public bool IncludeEnded { get; init; }
}

public sealed record AddCompanyEmployeeRequest(
    string? Email,
    string? FullName,
    string? JobTitle);

public sealed record UpdateCompanyEmployeeRequest(
    string? FullName,
    string? JobTitle,
    string? RowVersion);

public sealed record EmployeeRowVersionRequest(string? RowVersion);

public sealed record ReplaceCompanyEmployeeRolesRequest(
    IReadOnlyList<Guid>? RoleIds);

public sealed record TransferPrimaryContactRequest(
    string? TargetEmployeeRowVersion);
