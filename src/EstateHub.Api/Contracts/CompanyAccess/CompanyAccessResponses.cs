using EstateHub.Application.CompanyAccess;
using EstateHub.Domain.Enums;

namespace EstateHub.Api.Contracts.CompanyAccess;

public sealed record CompanyAccessCompanyResponse(
    Guid Id,
    string Slug,
    string DisplayName,
    string CompanyType,
    Guid? LogoFileAssetId,
    string TimeZoneId,
    string BaseCurrencyCode,
    DateTimeOffset VerifiedAt)
{
    public static CompanyAccessCompanyResponse From(CompanyAccessCompany company)
    {
        return new CompanyAccessCompanyResponse(
            company.Id,
            company.Slug,
            company.DisplayName,
            CompanyAccessEnumText.From(company.CompanyType),
            company.LogoFileAssetId,
            company.TimeZoneId,
            company.BaseCurrencyCode,
            company.VerifiedAt);
    }
}

public sealed record CompanyAccessEmployeeResponse(
    Guid Id,
    string FullName,
    string? JobTitle,
    bool IsPrimaryContact,
    DateTimeOffset JoinedAt)
{
    public static CompanyAccessEmployeeResponse From(CompanyAccessEmployee employee)
    {
        return new CompanyAccessEmployeeResponse(
            employee.Id,
            employee.FullName,
            employee.JobTitle,
            employee.IsPrimaryContact,
            employee.JoinedAt);
    }
}

public sealed record CompanyAccessRoleResponse(
    Guid Id,
    string Name,
    bool IsBuiltIn)
{
    public static CompanyAccessRoleResponse From(CompanyAccessRole role)
    {
        return new CompanyAccessRoleResponse(
            role.Id,
            role.Name,
            role.IsBuiltIn);
    }
}

public sealed record CompanyAccessContextResponse(
    CompanyAccessCompanyResponse Company,
    CompanyAccessEmployeeResponse Employee,
    IReadOnlyList<CompanyAccessRoleResponse> Roles,
    IReadOnlyList<string> PermissionCodes)
{
    public static CompanyAccessContextResponse From(CompanyAccessContext context)
    {
        return new CompanyAccessContextResponse(
            CompanyAccessCompanyResponse.From(context.Company),
            CompanyAccessEmployeeResponse.From(context.Employee),
            context.Roles.Select(CompanyAccessRoleResponse.From).ToArray(),
            context.PermissionCodes.ToArray());
    }
}

internal static class CompanyAccessEnumText
{
    public static string From(CompanyType value)
    {
        return value switch
        {
            CompanyType.Developer => "Developer",
            CompanyType.BrokerAgency => "BrokerAgency",
            _ => throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Unsupported CompanyType value.")
        };
    }
}
