using EstateHub.Application.Common;
using EstateHub.Application.PlatformCompanyApplications;
using EstateHub.Domain.Enums;

namespace EstateHub.Api.Contracts.PlatformCompanyApplications;

public sealed class PlatformRowVersionRequest
{
    public string? RowVersion { get; set; }
}

public sealed class PlatformDocumentVerificationRequest
{
    public string? ApplicationRowVersion { get; set; }
    public string? VerificationStatus { get; set; }
    public string? RejectionReason { get; set; }
}

public sealed class PlatformDecisionRequest
{
    public string? RowVersion { get; set; }
    public string? Reason { get; set; }
}

public sealed class PlatformCompanyApprovalRequest
{
    public string? RowVersion { get; set; }
    public string? Slug { get; set; }
    public string? DisplayName { get; set; }
    public Guid LocationId { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? TimeZoneId { get; set; }
    public string? BaseCurrencyCode { get; set; }
}

public sealed record PlatformCompanyApplicationSummaryResponse(
    Guid Id,
    Guid ApplicantApplicationUserId,
    string? ApplicantName,
    string? ApplicantEmail,
    string LegalName,
    string RegistrationNumber,
    string BusinessEmail,
    string CompanyType,
    string Status,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ReviewedAt,
    Guid? ReviewedByApplicationUserId,
    string? DecisionReason,
    int DocumentCount,
    int PendingDocumentCount,
    int ApprovedDocumentCount,
    int RejectedDocumentCount,
    Guid? ApprovedCompanyId,
    string RowVersion)
{
    public static PlatformCompanyApplicationSummaryResponse From(
        PlatformCompanyApplicationSummary item) => new(
            item.Id,
            item.ApplicantApplicationUserId,
            item.ApplicantName,
            item.ApplicantEmail,
            item.LegalName,
            item.RegistrationNumber,
            item.BusinessEmail,
            PlatformCompanyApplicationEnumText.CompanyType(item.CompanyType),
            PlatformCompanyApplicationEnumText.ApplicationStatus(item.Status),
            item.SubmittedAt,
            item.ReviewedAt,
            item.ReviewedByApplicationUserId,
            item.DecisionReason,
            item.DocumentCount,
            item.PendingDocumentCount,
            item.ApprovedDocumentCount,
            item.RejectedDocumentCount,
            item.ApprovedCompanyId,
            Convert.ToBase64String(item.RowVersion));
}

public sealed record PlatformCompanyApplicationDirectoryResponse(
    IReadOnlyList<PlatformCompanyApplicationSummaryResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public static PlatformCompanyApplicationDirectoryResponse From(
        PagedResult<PlatformCompanyApplicationSummary> result) => new(
            result.Items.Select(PlatformCompanyApplicationSummaryResponse.From).ToList(),
            result.PageNumber,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            result.HasPreviousPage,
            result.HasNextPage);
}

public sealed record PlatformApprovedCompanySummaryResponse(
    Guid Id,
    string Slug,
    string DisplayName)
{
    public static PlatformApprovedCompanySummaryResponse From(
        PlatformApprovedCompanySummary company) => new(
            company.Id,
            company.Slug,
            company.DisplayName);
}

public sealed record PlatformCompanyApplicationDocumentResponse(
    Guid Id,
    string DocumentType,
    Guid FileAssetId,
    string? OriginalFileName,
    string ContentType,
    long SizeBytes,
    string VerificationStatus,
    DateTimeOffset? ReviewedAt,
    Guid? ReviewedByApplicationUserId,
    string? RejectionReason)
{
    public static PlatformCompanyApplicationDocumentResponse From(
        PlatformCompanyApplicationDocument document) => new(
            document.Id,
            document.DocumentType,
            document.FileAssetId,
            document.OriginalFileName,
            document.ContentType,
            document.SizeBytes,
            PlatformCompanyApplicationEnumText.VerificationStatus(document.VerificationStatus),
            document.ReviewedAt,
            document.ReviewedByApplicationUserId,
            document.RejectionReason);
}

public sealed record PlatformCompanyApplicationHistoryResponse(
    Guid Id,
    string? FromStatus,
    string ToStatus,
    Guid? ChangedByApplicationUserId,
    DateTimeOffset ChangedAt,
    string? Reason)
{
    public static PlatformCompanyApplicationHistoryResponse From(
        PlatformCompanyApplicationStatusChange history) => new(
            history.Id,
            history.FromStatus is null
                ? null
                : PlatformCompanyApplicationEnumText.ApplicationStatus(history.FromStatus.Value),
            PlatformCompanyApplicationEnumText.ApplicationStatus(history.ToStatus),
            history.ChangedByApplicationUserId,
            history.ChangedAt,
            history.Reason);
}

public sealed record PlatformCompanyApplicationDetailsResponse(
    Guid Id,
    Guid ApplicantApplicationUserId,
    string? ApplicantName,
    string? ApplicantEmail,
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
    Guid? ReviewedByApplicationUserId,
    string? DecisionReason,
    PlatformApprovedCompanySummaryResponse? ApprovedCompany,
    string RowVersion,
    IReadOnlyList<PlatformCompanyApplicationDocumentResponse> Documents,
    IReadOnlyList<PlatformCompanyApplicationHistoryResponse> StatusHistory)
{
    public static PlatformCompanyApplicationDetailsResponse From(
        PlatformCompanyApplicationDetails application) => new(
            application.Id,
            application.ApplicantApplicationUserId,
            application.ApplicantName,
            application.ApplicantEmail,
            application.LegalName,
            application.BusinessEmail,
            application.PhoneNumber,
            application.RegistrationNumber,
            application.TaxId,
            application.Website,
            PlatformCompanyApplicationEnumText.CompanyType(application.CompanyType),
            application.OfficeAddress,
            application.LocationText,
            application.EstimatedPropertyRange,
            PlatformCompanyApplicationEnumText.ApplicationStatus(application.Status),
            application.SubmittedAt,
            application.ReviewedAt,
            application.ReviewedByApplicationUserId,
            application.DecisionReason,
            application.ApprovedCompany is null
                ? null
                : PlatformApprovedCompanySummaryResponse.From(application.ApprovedCompany),
            Convert.ToBase64String(application.RowVersion),
            application.Documents.Select(PlatformCompanyApplicationDocumentResponse.From).ToList(),
            application.StatusHistory.Select(PlatformCompanyApplicationHistoryResponse.From).ToList());
}

public sealed record PlatformDocumentDecisionResponse(
    string ApplicationRowVersion,
    PlatformCompanyApplicationDocumentResponse Document)
{
    public static PlatformDocumentDecisionResponse From(
        PlatformDocumentDecisionResult result) => new(
            Convert.ToBase64String(result.ApplicationRowVersion),
            PlatformCompanyApplicationDocumentResponse.From(result.Document));
}

public sealed record PlatformApprovedCompanyResponse(
    Guid CompanyId,
    string Slug,
    string DisplayName,
    Guid CompanyEmployeeId,
    string PrimaryContactFullName,
    bool OwnerBootstrapRequired)
{
    public static PlatformApprovedCompanyResponse From(
        PlatformApprovedCompanyResult result) => new(
            result.CompanyId,
            result.Slug,
            result.DisplayName,
            result.CompanyEmployeeId,
            result.PrimaryContactFullName,
            result.OwnerBootstrapRequired);
}

internal static class PlatformCompanyApplicationEnumText
{
    public static string CompanyType(CompanyType value) => value switch
    {
        EstateHub.Domain.Enums.CompanyType.Developer => "Developer",
        EstateHub.Domain.Enums.CompanyType.BrokerAgency => "BrokerAgency",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown company type.")
    };

    public static string ApplicationStatus(CompanyApplicationStatus value) => value switch
    {
        CompanyApplicationStatus.Draft => "Draft",
        CompanyApplicationStatus.Submitted => "Submitted",
        CompanyApplicationStatus.UnderReview => "UnderReview",
        CompanyApplicationStatus.NeedsChanges => "NeedsChanges",
        CompanyApplicationStatus.Approved => "Approved",
        CompanyApplicationStatus.Rejected => "Rejected",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown application status.")
    };

    public static string VerificationStatus(DocumentVerificationStatus value) => value switch
    {
        DocumentVerificationStatus.Pending => "Pending",
        DocumentVerificationStatus.Approved => "Approved",
        DocumentVerificationStatus.Rejected => "Rejected",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown document verification status.")
    };
}

