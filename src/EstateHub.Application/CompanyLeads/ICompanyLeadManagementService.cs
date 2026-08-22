using EstateHub.Application.Common;

namespace EstateHub.Application.CompanyLeads;

public interface ICompanyLeadManagementService
{
    Task<CompanyLeadQueryResult<PagedResult<CompanyLeadSummary>>> GetLeadsAsync(Guid userId, CompanyLeadDirectoryQuery query, CancellationToken cancellationToken = default);
    Task<CompanyLeadQueryResult<CompanyLeadDetails>> GetLeadAsync(Guid userId, Guid leadId, CancellationToken cancellationToken = default);
    Task<CompanyLeadMutationResult> CreateLeadAsync(Guid userId, CreateCompanyLeadCommand command, CancellationToken cancellationToken = default);
    Task<CompanyLeadMutationResult> UpdateLeadAsync(Guid userId, Guid leadId, UpdateCompanyLeadCommand command, CancellationToken cancellationToken = default);
    Task<CompanyLeadOperationStatus> ChangeStageAsync(Guid userId, Guid leadId, ChangeCompanyLeadStageCommand command, CancellationToken cancellationToken = default);
    Task<CompanyLeadRequirementResult> CreateRequirementAsync(Guid userId, Guid leadId, CompanyLeadRequirementCommand command, CancellationToken cancellationToken = default);
    Task<CompanyLeadRequirementResult> UpdateRequirementAsync(Guid userId, Guid leadId, Guid requirementId, CompanyLeadRequirementCommand command, CancellationToken cancellationToken = default);
    Task<CompanyLeadOperationStatus> ConfirmRequirementAsync(Guid userId, Guid leadId, Guid requirementId, byte[] leadRowVersion, CancellationToken cancellationToken = default);
    Task<CompanyLeadOperationStatus> DeleteRequirementAsync(Guid userId, Guid leadId, Guid requirementId, byte[] leadRowVersion, CancellationToken cancellationToken = default);
    Task<CompanyLeadInterestsResult> ReplaceInterestsAsync(Guid userId, Guid leadId, ReplaceCompanyLeadInterestsCommand command, CancellationToken cancellationToken = default);
    Task<CompanyLeadQueryResult<PagedResult<CompanyLeadActivity>>> GetActivitiesAsync(Guid userId, Guid leadId, CompanyLeadActivityQuery query, CancellationToken cancellationToken = default);
    Task<CompanyLeadActivityResult> CreateActivityAsync(Guid userId, Guid leadId, CreateCompanyLeadActivityCommand command, CancellationToken cancellationToken = default);
}
