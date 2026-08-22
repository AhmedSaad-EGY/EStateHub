using EstateHub.Application.Common;

namespace EstateHub.Application.PlatformCompanyApplications;

public interface IPlatformCompanyApplicationService
{
    Task<PlatformCompanyApplicationResult<PagedResult<PlatformCompanyApplicationSummary>>> GetApplicationsAsync(
        PlatformCompanyApplicationDirectoryQuery query,
        CancellationToken cancellationToken = default);

    Task<PlatformCompanyApplicationResult<PlatformCompanyApplicationDetails>> GetApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<PlatformCompanyApplicationResult<PlatformCompanyApplicationContent>> GetDocumentContentAsync(
        Guid applicationId,
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<PlatformCompanyApplicationOperationStatus> StartReviewAsync(
        Guid actingPlatformAdminId,
        Guid applicationId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default);

    Task<PlatformCompanyApplicationResult<PlatformDocumentDecisionResult>> VerifyDocumentAsync(
        Guid actingPlatformAdminId,
        Guid applicationId,
        Guid documentId,
        PlatformDocumentDecisionCommand command,
        CancellationToken cancellationToken = default);

    Task<PlatformCompanyApplicationOperationStatus> RequestChangesAsync(
        Guid actingPlatformAdminId,
        Guid applicationId,
        byte[] rowVersion,
        string reason,
        CancellationToken cancellationToken = default);

    Task<PlatformCompanyApplicationOperationStatus> RejectAsync(
        Guid actingPlatformAdminId,
        Guid applicationId,
        byte[] rowVersion,
        string reason,
        CancellationToken cancellationToken = default);

    Task<PlatformCompanyApplicationResult<PlatformApprovedCompanyResult>> ApproveAsync(
        Guid actingPlatformAdminId,
        Guid applicationId,
        PlatformCompanyApprovalCommand command,
        CancellationToken cancellationToken = default);
}
