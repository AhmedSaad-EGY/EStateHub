using EstateHub.Application.Common;

namespace EstateHub.Application.CompanyApplications;

public interface ICompanyApplicationService
{
    Task<PagedResult<CompanyApplicationSummary>> GetApplicationsAsync(
        Guid applicationUserId,
        CompanyApplicationDirectoryQuery query,
        CancellationToken cancellationToken = default);

    Task<CompanyApplicationDetails?> GetApplicationAsync(
        Guid applicationUserId,
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<CompanyApplicationMutationResult> CreateApplicationAsync(
        Guid applicationUserId,
        CompanyApplicationCommand command,
        CancellationToken cancellationToken = default);

    Task<CompanyApplicationMutationResult> UpdateApplicationAsync(
        Guid applicationUserId,
        Guid applicationId,
        CompanyApplicationCommand command,
        CancellationToken cancellationToken = default);

    Task<CompanyApplicationDocumentsResult> ReplaceDocumentsAsync(
        Guid applicationUserId,
        Guid applicationId,
        IReadOnlyList<CompanyApplicationDocumentCommand> documents,
        byte[] rowVersion,
        CancellationToken cancellationToken = default);

    Task<CompanyApplicationOperationStatus> SubmitApplicationAsync(
        Guid applicationUserId,
        Guid applicationId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default);
}
