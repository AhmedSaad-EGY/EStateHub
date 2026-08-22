using EstateHub.Application.Common;
using EstateHub.Domain.Enums;

namespace EstateHub.Application.CompanyApplications;

public sealed record CompanyApplicationDirectoryQuery(
    int PageNumber,
    int PageSize,
    CompanyApplicationStatus? Status);

public sealed record CompanyApplicationSummary(
    Guid Id,
    string LegalName,
    CompanyType CompanyType,
    CompanyApplicationStatus Status,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ReviewedAt,
    string? DecisionReason,
    int DocumentCount,
    Guid? ApprovedCompanyId,
    byte[] RowVersion);

public sealed record CompanyApplicationCommand(
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
    byte[]? RowVersion);

public sealed record CompanyApplicationApprovedCompany(
    Guid Id,
    string Slug,
    string DisplayName);

public sealed record CompanyApplicationDocument(
    Guid Id,
    string DocumentType,
    Guid FileAssetId,
    string? OriginalFileName,
    string ContentType,
    long SizeBytes,
    DocumentVerificationStatus VerificationStatus,
    DateTimeOffset? ReviewedAt,
    string? RejectionReason);

public sealed record CompanyApplicationStatusChange(
    Guid Id,
    CompanyApplicationStatus? FromStatus,
    CompanyApplicationStatus ToStatus,
    DateTimeOffset ChangedAt,
    string? Reason);

public sealed record CompanyApplicationDetails(
    Guid Id,
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
    string? DecisionReason,
    CompanyApplicationApprovedCompany? ApprovedCompany,
    byte[] RowVersion,
    IReadOnlyList<CompanyApplicationDocument> Documents,
    IReadOnlyList<CompanyApplicationStatusChange> StatusHistory);

public sealed record CompanyApplicationDocumentCommand(
    Guid FileAssetId,
    string DocumentType);

public enum CompanyApplicationOperationStatus
{
    Succeeded,
    NotFound,
    Conflict,
    ServiceUnavailable
}

public sealed record CompanyApplicationMutationResult(
    CompanyApplicationOperationStatus Status,
    CompanyApplicationDetails? Application = null);

public sealed record CompanyApplicationDocumentsResult(
    CompanyApplicationOperationStatus Status,
    byte[]? RowVersion = null,
    IReadOnlyList<CompanyApplicationDocument>? Documents = null);

