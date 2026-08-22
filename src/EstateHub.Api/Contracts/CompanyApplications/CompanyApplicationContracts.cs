using EstateHub.Application.Common;
using EstateHub.Application.CompanyApplications;
using EstateHub.Domain.Enums;

namespace EstateHub.Api.Contracts.CompanyApplications;

public abstract class CompanyApplicationFieldsRequest
{
    public string? LegalName { get; set; }
    public string? BusinessEmail { get; set; }
    public string? PhoneNumber { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxId { get; set; }
    public string? Website { get; set; }
    public string? CompanyType { get; set; }
    public string? OfficeAddress { get; set; }
    public string? LocationText { get; set; }
    public string? EstimatedPropertyRange { get; set; }
}

public sealed class CreateCompanyApplicationRequest : CompanyApplicationFieldsRequest;

public sealed class UpdateCompanyApplicationRequest : CompanyApplicationFieldsRequest
{
    public string? RowVersion { get; set; }
}

public sealed class ReplaceCompanyApplicationDocumentsRequest
{
    public string? RowVersion { get; set; }
    public IReadOnlyList<CompanyApplicationDocumentRequest>? Documents { get; set; }
}

public sealed class CompanyApplicationDocumentRequest
{
    public Guid FileAssetId { get; set; }
    public string? DocumentType { get; set; }
}

public sealed class SubmitCompanyApplicationRequest
{
    public string? RowVersion { get; set; }
}

public sealed record CompanyApplicationSummaryResponse(
    Guid Id,
    string LegalName,
    string CompanyType,
    string Status,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ReviewedAt,
    string? DecisionReason,
    int DocumentCount,
    Guid? ApprovedCompanyId,
    string RowVersion)
{
    public static CompanyApplicationSummaryResponse From(CompanyApplicationSummary application) => new(
        application.Id,
        application.LegalName,
        CompanyApplicationEnumText.CompanyType(application.CompanyType),
        CompanyApplicationEnumText.Status(application.Status),
        application.SubmittedAt,
        application.ReviewedAt,
        application.DecisionReason,
        application.DocumentCount,
        application.ApprovedCompanyId,
        Convert.ToBase64String(application.RowVersion));
}

public sealed record CompanyApplicationDirectoryResponse(
    IReadOnlyList<CompanyApplicationSummaryResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public static CompanyApplicationDirectoryResponse From(
        PagedResult<CompanyApplicationSummary> result) => new(
            result.Items.Select(CompanyApplicationSummaryResponse.From).ToList(),
            result.PageNumber,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            result.HasPreviousPage,
            result.HasNextPage);
}

public sealed record CompanyApplicationApprovedCompanyResponse(
    Guid Id,
    string Slug,
    string DisplayName)
{
    public static CompanyApplicationApprovedCompanyResponse From(
        CompanyApplicationApprovedCompany company) => new(
            company.Id,
            company.Slug,
            company.DisplayName);
}

public sealed record CompanyApplicationDocumentResponse(
    Guid Id,
    string DocumentType,
    Guid FileAssetId,
    string? OriginalFileName,
    string ContentType,
    long SizeBytes,
    string VerificationStatus,
    DateTimeOffset? ReviewedAt,
    string? RejectionReason,
    string ContentPath)
{
    public static CompanyApplicationDocumentResponse From(
        CompanyApplicationDocument document) => new(
            document.Id,
            document.DocumentType,
            document.FileAssetId,
            document.OriginalFileName,
            document.ContentType,
            document.SizeBytes,
            CompanyApplicationEnumText.VerificationStatus(document.VerificationStatus),
            document.ReviewedAt,
            document.RejectionReason,
            $"/api/me/files/{document.FileAssetId}/content");
}

public sealed record CompanyApplicationStatusHistoryResponse(
    Guid Id,
    string? FromStatus,
    string ToStatus,
    DateTimeOffset ChangedAt,
    string? Reason)
{
    public static CompanyApplicationStatusHistoryResponse From(
        CompanyApplicationStatusChange history) => new(
            history.Id,
            history.FromStatus is null
                ? null
                : CompanyApplicationEnumText.Status(history.FromStatus.Value),
            CompanyApplicationEnumText.Status(history.ToStatus),
            history.ChangedAt,
            history.Reason);
}

public sealed record CompanyApplicationDetailsResponse(
    Guid Id,
    string LegalName,
    string BusinessEmail,
    string PhoneNumber,
    string RegistrationNumber,
    string? TaxId,
    string? Website,
    string CompanyType,
    string OfficeAddress,
    string LocationText,
    string? EstimatedPropertyRange,
    string Status,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ReviewedAt,
    string? DecisionReason,
    CompanyApplicationApprovedCompanyResponse? ApprovedCompany,
    string RowVersion,
    IReadOnlyList<CompanyApplicationDocumentResponse> Documents,
    IReadOnlyList<CompanyApplicationStatusHistoryResponse> StatusHistory)
{
    public static CompanyApplicationDetailsResponse From(
        CompanyApplicationDetails application) => new(
            application.Id,
            application.LegalName,
            application.BusinessEmail,
            application.PhoneNumber,
            application.RegistrationNumber,
            application.TaxId,
            application.Website,
            CompanyApplicationEnumText.CompanyType(application.CompanyType),
            application.OfficeAddress,
            application.LocationText,
            application.EstimatedPropertyRange,
            CompanyApplicationEnumText.Status(application.Status),
            application.SubmittedAt,
            application.ReviewedAt,
            application.DecisionReason,
            application.ApprovedCompany is null
                ? null
                : CompanyApplicationApprovedCompanyResponse.From(application.ApprovedCompany),
            Convert.ToBase64String(application.RowVersion),
            application.Documents.Select(CompanyApplicationDocumentResponse.From).ToList(),
            application.StatusHistory.Select(CompanyApplicationStatusHistoryResponse.From).ToList());
}

public sealed record CompanyApplicationDocumentsResponse(
    string RowVersion,
    IReadOnlyList<CompanyApplicationDocumentResponse> Documents)
{
    public static CompanyApplicationDocumentsResponse From(
        CompanyApplicationDocumentsResult result) => new(
            Convert.ToBase64String(result.RowVersion!),
            result.Documents!.Select(CompanyApplicationDocumentResponse.From).ToList());
}

internal static class CompanyApplicationEnumText
{
    public static string CompanyType(CompanyType value) => value switch
    {
        Domain.Enums.CompanyType.Developer => "Developer",
        Domain.Enums.CompanyType.BrokerAgency => "BrokerAgency",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown company type.")
    };

    public static string Status(CompanyApplicationStatus value) => value switch
    {
        CompanyApplicationStatus.Draft => "Draft",
        CompanyApplicationStatus.Submitted => "Submitted",
        CompanyApplicationStatus.UnderReview => "UnderReview",
        CompanyApplicationStatus.NeedsChanges => "NeedsChanges",
        CompanyApplicationStatus.Approved => "Approved",
        CompanyApplicationStatus.Rejected => "Rejected",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown company application status.")
    };

    public static string VerificationStatus(DocumentVerificationStatus value) => value switch
    {
        DocumentVerificationStatus.Pending => "Pending",
        DocumentVerificationStatus.Approved => "Approved",
        DocumentVerificationStatus.Rejected => "Rejected",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown document verification status.")
    };
}

