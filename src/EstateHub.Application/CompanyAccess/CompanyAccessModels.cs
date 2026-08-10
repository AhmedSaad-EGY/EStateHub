using EstateHub.Domain.Enums;

namespace EstateHub.Application.CompanyAccess;

public sealed record CompanyAccessCompany(
    Guid Id,
    string Slug,
    string DisplayName,
    CompanyType CompanyType,
    Guid? LogoFileAssetId,
    string TimeZoneId,
    string BaseCurrencyCode,
    DateTimeOffset VerifiedAt);

public sealed record CompanyAccessEmployee(
    Guid Id,
    string FullName,
    string? JobTitle,
    bool IsPrimaryContact,
    DateTimeOffset JoinedAt);

public sealed record CompanyAccessRole(
    Guid Id,
    string Name,
    bool IsBuiltIn);

public sealed record CompanyAccessContext(
    CompanyAccessCompany Company,
    CompanyAccessEmployee Employee,
    IReadOnlyList<CompanyAccessRole> Roles,
    IReadOnlyList<string> PermissionCodes);

public sealed record CompanyAccessScope(
    Guid CompanyId,
    Guid CompanyEmployeeId);
