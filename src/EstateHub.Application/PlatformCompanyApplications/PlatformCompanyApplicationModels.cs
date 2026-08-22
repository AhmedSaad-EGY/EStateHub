using System.IO;
using EstateHub.Application.Common;
using EstateHub.Domain.Enums;

namespace EstateHub.Application.PlatformCompanyApplications;

public sealed record PlatformCompanyApplicationDirectoryQuery(
    int PageNumber,
    int PageSize,
    string? Search,
    CompanyApplicationStatus? Status,
    CompanyType? CompanyType,
    DateTimeOffset? SubmittedFrom,
    DateTimeOffset? SubmittedTo);

public sealed record PlatformCompanyApplicationSummary(
    Guid Id,
    Guid ApplicantApplicationUserId,
    string? ApplicantName,
    string? ApplicantEmail,
    string LegalName,
    string RegistrationNumber,
    string BusinessEmail,
    CompanyType CompanyType,
    CompanyApplicationStatus Status,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ReviewedAt,
    Guid? ReviewedByApplicationUserId,
    string? DecisionReason,
    int DocumentCount,
    int PendingDocumentCount,
    int ApprovedDocumentCount,
    int RejectedDocumentCount,
    Guid? ApprovedCompanyId,
    byte[] RowVersion);

public sealed record PlatformApprovedCompanySummary(
    Guid Id,
    string Slug,
    string DisplayName);

public sealed record PlatformCompanyApplicationDocument(
    Guid Id,
    string DocumentType,
    Guid FileAssetId,
    string? OriginalFileName,
    string ContentType,
    long SizeBytes,
    DocumentVerificationStatus VerificationStatus,
    DateTimeOffset? ReviewedAt,
    Guid? ReviewedByApplicationUserId,
    string? RejectionReason);

public sealed record PlatformCompanyApplicationStatusChange(
    Guid Id,
    CompanyApplicationStatus? FromStatus,
    CompanyApplicationStatus ToStatus,
    Guid? ChangedByApplicationUserId,
    DateTimeOffset ChangedAt,
    string? Reason);

public sealed record PlatformCompanyApplicationDetails(
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
    CompanyType CompanyType,
    string OfficeAddress,
    string LocationText,
    string? EstimatedPropertyRange,
    CompanyApplicationStatus Status,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ReviewedAt,
    Guid? ReviewedByApplicationUserId,
    string? DecisionReason,
    PlatformApprovedCompanySummary? ApprovedCompany,
    byte[] RowVersion,
    IReadOnlyList<PlatformCompanyApplicationDocument> Documents,
    IReadOnlyList<PlatformCompanyApplicationStatusChange> StatusHistory);

public sealed record PlatformCompanyApplicationContent(
    Stream Content,
    string ContentType,
    string DownloadFileName);

public sealed record PlatformDocumentDecisionCommand(
    DocumentVerificationStatus VerificationStatus,
    string? RejectionReason,
    byte[] ApplicationRowVersion);

public sealed record PlatformDocumentDecisionResult(
    byte[] ApplicationRowVersion,
    PlatformCompanyApplicationDocument Document);

public sealed record PlatformCompanyApprovalCommand(
    byte[] RowVersion,
    string Slug,
    string DisplayName,
    Guid LocationId,
    string AddressLine1,
    string? AddressLine2,
    string? PostalCode,
    decimal? Latitude,
    decimal? Longitude,
    string TimeZoneId,
    string BaseCurrencyCode);

public sealed record PlatformApprovedCompanyResult(
    Guid CompanyId,
    string Slug,
    string DisplayName,
    Guid CompanyEmployeeId,
    string PrimaryContactFullName,
    bool OwnerBootstrapRequired);

public enum PlatformCompanyApplicationOperationStatus
{
    Succeeded,
    NotFound,
    Conflict,
    ServiceUnavailable
}

public sealed record PlatformCompanyApplicationResult<T>(
    PlatformCompanyApplicationOperationStatus Status,
    T? Value = default);

